# SB.API_SB — API RESTful y portal de mantenimientos y nómina

Solución completa para la prueba técnica de la **Superintendencia de Bancos de la
República Dominicana**: una API RESTful en .NET 8 con arquitectura Onion y una
aplicación web en React + TypeScript que consume esa API.

La solución cubre tres módulos:

1. **Entidades gubernamentales** — mantenimiento del listado oficial de la
   República Dominicana (181 registros), persistido en un **archivo de texto
   plano** ubicado dentro del propio proyecto.
2. **Empleados y nómina** — gestión de empleados con cálculo automático del pago
   semanal según cuatro tipos de contrato, con filtros por nombre, departamento y
   estado, y reporte semanal detallado.
3. **Usuarios y roles** — autenticación con **JWT** (`Authorization: Bearer`) y
   autorización por rol.

---

## 1. Requisitos previos

| Herramienta | Versión mínima | Notas |
|---|---|---|
| .NET SDK | 8.0 | Los proyectos se compilan contra `net8.0`. |
| Node.js | 18.x | Probado con Node 18.20 y npm 10.8. |
| Git | 2.x | — |
| SQL Server | 2019 (opcional) | Solo si desea usar SQL Server en lugar de SQLite. |

> **Nota sobre el runtime.** Los proyectos ejecutables declaran
> `<RollForward>LatestMajor</RollForward>`, por lo que la solución compila contra
> .NET 8 y también se ejecuta en máquinas donde solo esté instalado un runtime
> mayor (.NET 9 o .NET 10). Si el runtime de .NET 8 está presente, se usa ese.

Verifique las herramientas:

```bash
dotnet --version && node --version && npm --version
```

---

## 2. Ejecución rápida

### 2.1 Backend (API)

```bash
dotnet restore && dotnet build
```

```bash
dotnet run --project src/SB.API_SB.Presentation
```

La API queda disponible en **http://localhost:5080** y la documentación
interactiva de Swagger en **http://localhost:5080/swagger**.

En el primer arranque, la aplicación:

- genera `src/SB.API_SB.Presentation/Database/GovernmentEntities.txt` a partir del
  archivo semilla con las 181 entidades gubernamentales;
- crea la base de datos relacional (SQLite por defecto) y siembra roles, el
  usuario administrador, cinco departamentos y cinco empleados de demostración
  que ejercitan los cuatro tipos de cálculo de nómina.

### 2.2 Frontend (portal web)

En una segunda terminal:

```bash
cd frontend && npm install
```

```bash
npm run dev
```

El portal queda disponible en **http://localhost:5173**. El servidor de
desarrollo redirige `/api` hacia `http://localhost:5080`, por lo que no hace
falta configurar nada más.

### 2.3 Credenciales iniciales

| Usuario | Contraseña | Rol |
|---|---|---|
| `administrador` | `Sb2024Admin` | Administrador |

La contraseña se define en la sección `Seed` de `appsettings.json`. **Cámbiela
antes de cualquier despliegue** y trasládela a variables de entorno o a
`dotnet user-secrets`.

---

## 3. Estructura de la solución

```
SB.API_SB.sln
├── src/
│   ├── SB.API_SB.Domain            → Núcleo: entidades, reglas de negocio, contratos
│   ├── SB.API_SB.Application       → Casos de uso: DTO, interfaces, validaciones, mapeos
│   ├── SB.API_SB.Services          → Implementación de los casos de uso
│   ├── SB.API_SB.Infrastructure    → EF Core, archivo de texto plano, JWT, hashing
│   └── SB.API_SB.Presentation      → Host de la API: controladores, middlewares, Swagger
│       └── Database/               → Base de datos de texto plano (dentro del proyecto)
├── tests/
│   └── SB.API_SB.Tests             → 77 pruebas unitarias (xUnit + NSubstitute)
├── frontend/                       → Aplicación React + TypeScript (Vite)
└── docs/                           → Reporte técnico y respuestas de conceptualización
```

La dirección de las dependencias es siempre hacia el centro:

```
Presentation ──► Services ──► Application ──► Domain
      │              │              │
      └──────────────┴──► Infrastructure ──┘
```

El **Dominio no referencia ningún proyecto ni paquete de infraestructura**: es el
invariante que sostiene la arquitectura.

---

## 4. Configuración

Toda la configuración vive en `src/SB.API_SB.Presentation/appsettings.json`.
**Ninguna cadena de conexión ni clave está escrita en el código.**

| Sección | Clave | Descripción |
|---|---|---|
| `ConnectionStrings` | `SqlServerConnection` | Cadena de conexión de SQL Server. |
| `ConnectionStrings` | `SqliteConnection` | Cadena de conexión de SQLite (desarrollo). |
| `Database` | `Provider` | `Sqlite` (por defecto) o `SqlServer`. |
| `Database` | `ApplyAutomaticInitialization` | Crea el esquema y siembra datos al iniciar. |
| `FlatFileDatabase` | `GovernmentEntitiesFilePath` | Ruta del archivo de datos, relativa al proyecto. |
| `FlatFileDatabase` | `GovernmentEntitiesSeedFilePath` | Ruta del archivo semilla. |
| `FlatFileDatabase` | `CreateBackupOnWrite` | Copia de respaldo antes de cada reescritura. |
| `Jwt` | `Issuer`, `Audience`, `SigningKey` | Parámetros del token. |
| `Jwt` | `AccessTokenExpirationMinutes` | Vigencia del token (120 por defecto). |
| `Seed` | `Administrator*` | Datos del usuario administrador inicial. |
| `Cors` | `AllowedOrigins` | Orígenes autorizados del portal web. |
| `Serilog` | — | Niveles y destinos del registro de eventos. |

### 4.1 Cambiar a SQL Server

1. Ajuste `ConnectionStrings:SqlServerConnection`.
2. Cambie `Database:Provider` a `"SqlServer"`.
3. Ejecute la API: el esquema se crea automáticamente en el primer arranque.

Ambos proveedores usan **el mismo `DbContext` y las mismas configuraciones de
mapeo**; solo cambia el proveedor registrado en el contenedor de dependencias.

### 4.2 Sobre los dos almacenes de datos

El documento de la prueba pide, en secciones distintas, un **archivo de texto
plano** y **SQL Server u Oracle**. La solución cumple ambas exigencias asignando
a cada almacén el módulo que le corresponde:

| Módulo | Almacén | Motivo |
|---|---|---|
| Entidades gubernamentales | Archivo de texto plano dentro del proyecto | Requisito explícito del mantenimiento solicitado. |
| Empleados, departamentos, usuarios y roles | Base de datos relacional con EF Core | Requieren relaciones, índices únicos y transacciones. |

Las dos implementaciones satisfacen contratos declarados en el Dominio
(`IGovernmentEntityRepository`, `IEmployeeRepository`, …), por lo que **la lógica
de negocio no sabe cuál tecnología la respalda**.

---

## 5. Pruebas

```bash
dotnet test
```

77 pruebas unitarias distribuidas así:

| Área | Archivo | Qué verifica |
|---|---|---|
| Cálculo de nómina | `PayrollCalculationTests` | Las cuatro fórmulas y el límite exacto de las 40 horas. |
| Validaciones por tipo | `EmployeeRequestValidationTests` | Que cada regla se active solo para el tipo que corresponde. |
| Reglas de negocio | `EmployeeServiceTests` | Duplicados, departamento inactivo, cambio de tipo prohibido. |
| Reporte de nómina | `PayrollReportServiceTests` | Totales, agrupaciones y el límite de 1,000 empleados en menos de 2 s. |
| Archivo de texto plano | `GovernmentEntityFileRepositoryTests` | CRUD real sobre disco, filtros y paginación. |
| Formato del archivo | `FlatFileRecordSerializerTests` | Escape reversible de `|`, `\` y saltos de línea. |
| Seguridad | `PasswordHasherTests` | Derivación PBKDF2, sales distintas y verificación. |
| Extensibilidad | `EmployeeTypeHandlerResolverTests` | Que todo tipo del enumerado tenga manejador registrado. |

Frontend:

```bash
cd frontend && npm run typecheck && npm run lint && npm run build
```

---

## 6. Endpoints principales

Todos los endpoints, salvo los indicados, exigen el encabezado
`Authorization: Bearer <token>`.

### Autenticación

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/autenticacion/iniciar-sesion` | Emite el token JWT. **Anónimo.** |
| `GET` | `/api/autenticacion/sesion-actual` | Devuelve la identidad del token. |

### Entidades gubernamentales

| Método | Ruta | Rol requerido |
|---|---|---|
| `GET` | `/api/entidades-gubernamentales` | Cualquier rol |
| `GET` | `/api/entidades-gubernamentales/catalogos` | Cualquier rol |
| `GET` | `/api/entidades-gubernamentales/{id}` | Cualquier rol |
| `POST` | `/api/entidades-gubernamentales` | Administrador o RecursosHumanos |
| `PUT` | `/api/entidades-gubernamentales/{id}` | Administrador o RecursosHumanos |
| `DELETE` | `/api/entidades-gubernamentales/{id}` | Administrador o RecursosHumanos |

Filtros aceptados en la consulta: `name`, `category`, `sector`, `stateBranch`,
`status`, `pageNumber`, `pageSize`.

### Empleados y nómina

| Método | Ruta | Rol requerido |
|---|---|---|
| `GET` | `/api/empleados` | Cualquier rol |
| `GET` | `/api/empleados/{id}` | Cualquier rol |
| `POST` | `/api/empleados` | Administrador o RecursosHumanos |
| `PUT` | `/api/empleados/{id}` | Administrador o RecursosHumanos |
| `DELETE` | `/api/empleados/{id}` | Administrador o RecursosHumanos |
| `GET` | `/api/reportes-nomina/semanal` | Cualquier rol |
| `GET` | `/api/departamentos` | Cualquier rol |

Filtros de empleados: `name`, `departmentId`, `status`, `type`, `pageNumber`,
`pageSize`.

### Usuarios

| Método | Ruta | Rol requerido |
|---|---|---|
| `GET` | `/api/usuarios` | Administrador |
| `GET` | `/api/usuarios/roles` | Cualquier rol |
| `POST` | `/api/usuarios` | Administrador |
| `PUT` | `/api/usuarios/{id}` | Administrador |
| `POST` | `/api/usuarios/cambiar-contrasena` | Cualquier rol (su propia cuenta) |
| `DELETE` | `/api/usuarios/{id}` | Administrador |

### Estado

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/estado` | Disponibilidad del servicio. **Anónimo.** |

---

## 7. Ejemplo de uso

Obtener el token:

```bash
curl -X POST http://localhost:5080/api/autenticacion/iniciar-sesion -H "Content-Type: application/json" -d "{\"userName\":\"administrador\",\"password\":\"Sb2024Admin\"}"
```

Consultar entidades filtrando por nombre (41 de las 181 contienen "Instituto"):

```bash
curl -H "Authorization: Bearer <TOKEN>" "http://localhost:5080/api/entidades-gubernamentales?name=Instituto&pageSize=5"
```

Registrar un empleado por horas:

```bash
curl -X POST http://localhost:5080/api/empleados -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json" -d "{\"type\":\"Hourly\",\"paternalLastName\":\"Diaz\",\"socialSecurityNumber\":\"001-9999999-9\",\"departmentId\":\"<GUID>\",\"status\":\"Active\",\"hourlyWage\":300,\"hoursWorked\":45}"
```

Respuesta (fragmento): el pago se calcula como `300 × 40 + 300 × 1.5 × 5`.

```json
{
  "weeklyPayment": 14250.00,
  "paymentBreakdown": {
    "formula": "pagoSemanal = (sueldoPorHora * 40) + (sueldoPorHora * 1.5 * (horasTrabajadas - 40))",
    "components": [
      { "concept": "Horas ordinarias", "detail": "40.00 horas x 300.00", "amount": 12000.00 },
      { "concept": "Horas extras", "detail": "5.00 horas x 300.00 x 1.5", "amount": 2250.00 }
    ],
    "totalAmount": 14250.00
  }
}
```

---

## 8. Reglas de cálculo de nómina

| Tipo | Campos capturados | Fórmula |
|---|---|---|
| Asalariado | primerNombre, apellidoPaterno, numeroSeguroSocial, salarioSemanal | `salarioSemanal` |
| Por horas | apellidoPaterno, numeroSeguroSocial, sueldoPorHora, horasTrabajadas | `≤ 40 h`: `sueldoPorHora × horas`<br>`> 40 h`: `(sueldoPorHora × 40) + (sueldoPorHora × 1.5 × (horas − 40))` |
| Por comisión | primerNombre, apellidoPaterno, numeroSeguroSocial, ventasBrutas, tarifaComision | `ventasBrutas × tarifaComision` |
| Asalariado por comisión | los anteriores más salarioBase | `(ventasBrutas × tarifaComision) + salarioBase + (salarioBase × 0.10)` |

Cada fórmula vive en la subclase de `Employee` que le corresponde y se resuelve
por polimorfismo. **La fórmula no se duplica en el frontend**: el portal muestra
el monto y el desglose que devuelve la API.

---

## 9. Registro de eventos (logs)

Serilog escribe en dos destinos:

- **Consola**, en formato compacto para desarrollo.
- **Archivo diario** en `src/SB.API_SB.Presentation/Logs/sb-api-sb-AAAAMMDD.log`,
  con rotación diaria, límite de 50 MB por archivo y retención de 30 días.

Se registra: el arranque y la siembra de datos, una línea por petición HTTP con
método, ruta, código de respuesta y duración, cada operación de negocio (altas,
cambios, eliminaciones) con el usuario responsable, los intentos de
autenticación fallidos, las validaciones rechazadas y la traza completa de
cualquier error no controlado. Cada evento lleva un **identificador de
correlación** que también se devuelve al cliente en las respuestas de error.

---

## 10. Convenciones de código aplicadas

| Regla | Aplicación |
|---|---|
| Clases en PascalCase | `EmployeeService`, `GovernmentEntityFileRepository` |
| Métodos en PascalCase | `CalculateWeeklyPayment`, `BuildPaymentBreakdown` |
| Enums en PascalCase | `EmployeeType`, `RecordStatus` |
| Propiedades en PascalCase | `WeeklySalary`, `SocialSecurityNumber` |
| Variables locales en camelCase | `weeklyPayment`, `normalizedName` |
| Parámetros en camelCase | `employeeId`, `cancellationToken` |
| Constantes en mayúscula | `STANDARD_WEEKLY_HOURS`, `OVERTIME_RATE_MULTIPLIER` |
| Interfaces con `I` inicial | `IEmployeeRepository`, `IPasswordHasher` |
| Sin abreviaturas | `department` (no `dept`), `configuration` (no `config`) |
| Sin números mágicos | Todos en `PayrollConstants`, `ValidationLimits`, `ColumnDefinitions` |
| Cadenas de conexión en AppSettings | Sección `ConnectionStrings` |

---

## 11. Documentación adicional

- [`docs/REPORTE-TECNICO.md`](docs/REPORTE-TECNICO.md) — arquitectura,
  metodologías, tecnologías empleadas y justificación de cada decisión.
- [`docs/RESPUESTAS-CONCEPTUALIZACION.md`](docs/RESPUESTAS-CONCEPTUALIZACION.md) —
  respuestas conceptuales redactadas en formato de correo.

---

## 12. Notas de entrega

- El **logotipo institucional** se dibuja como SVG en línea
  (`frontend/src/components/BrandLogo.tsx`) para no depender de un recurso
  externo. Si se dispone del archivo oficial del portal de la Superintendencia,
  basta sustituir el contenido de ese componente.
- Los **iconos** son SVG en línea (`frontend/src/components/Icons.tsx`),
  incluido el icono de inicio equivalente al adjunto en el requerimiento.
- Los **colores institucionales** indicados —azul `rgba(13, 48, 72, .9)` y gris
  `rgba(237, 240, 247)`— se declaran como variables CSS en
  `frontend/src/styles/global.css` y son la única fuente de la paleta.
- La base de datos de texto plano generada (`GovernmentEntities.txt`) está
  excluida del control de versiones; el archivo semilla
  (`GovernmentEntities.seed.txt`), que sí está versionado, es la fuente de verdad
  y permite clonar y ejecutar sin pasos manuales.
