# SB.API_SB — API RESTful y portal de mantenimientos y nómina

Solución completa para la prueba técnica de la **Superintendencia de Bancos de la
República Dominicana**: una API RESTful en .NET 8 con arquitectura Onion y una
aplicación web en React + TypeScript que consume esa API.

La solución cubre cuatro módulos:

1. **Entidades gubernamentales** — mantenimiento del listado oficial de la
   República Dominicana (181 registros), persistido en un **archivo de texto
   plano** ubicado dentro del propio proyecto.
2. **Compañías y empleados** — cada empleado pertenece a una compañía; captura de
   datos por tipo de contrato y filtros por nombre, compañía, departamento y estado.
3. **Cálculo de pagos semanales** — generación de la nómina de una semana por
   compañía, con historial de semanas anteriores. **Una semana solo puede pagarse
   una vez.**
4. **Usuarios, roles y registro de eventos** — autenticación con **JWT**
   (`Authorization: Bearer`), autorización por rol y consulta del log desde la
   propia aplicación (solo administrador).

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
  usuario administrador, cinco departamentos, **tres compañías**, **once empleados**
  que ejercitan los cuatro tipos de cálculo y **24 nóminas históricas** (ocho
  semanas anteriores por compañía) para que el historial tenga datos desde el
  primer momento.

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
│       ├── Database/               → Base de datos de texto plano (dentro del proyecto)
│       └── Logs/                   → Registro de Serilog (texto y JSON), consultable desde la app
├── tests/
│   └── SB.API_SB.Tests             → 98 pruebas unitarias (xUnit + NSubstitute)
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
| `EventLog` | `DirectoryPath` | Directorio de los archivos de registro, relativo al proyecto. |
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
| Compañías, empleados, departamentos, nóminas, usuarios y roles | Base de datos relacional con EF Core | Requieren relaciones, índices únicos y transacciones. |

Las dos implementaciones satisfacen contratos declarados en el Dominio
(`IGovernmentEntityRepository`, `IEmployeeRepository`, …), por lo que **la lógica
de negocio no sabe cuál tecnología la respalda**.

---

## 5. Pruebas

```bash
dotnet test
```

98 pruebas unitarias distribuidas así:

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
| Semana de nómina | `PayrollWeekTests` | Identidad ISO 8601 de la semana, límites de fin de año y años de 53 semanas. |
| Pago semanal | `PayrollRunServiceTests` | Que una semana no se pueda pagar dos veces, la instantánea del cálculo, la anulación y el límite de 1,000 empleados en menos de 2 s. |

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

### Compañías

| Método | Ruta | Rol requerido |
|---|---|---|
| `GET` | `/api/companias` | Cualquier rol |
| `GET` | `/api/companias/{id}` | Cualquier rol |
| `POST` | `/api/companias` | Administrador o RecursosHumanos |
| `PUT` | `/api/companias/{id}` | Administrador o RecursosHumanos |
| `DELETE` | `/api/companias/{id}` | Administrador |

### Empleados

| Método | Ruta | Rol requerido |
|---|---|---|
| `GET` | `/api/empleados` | Cualquier rol |
| `GET` | `/api/empleados/{id}` | Cualquier rol |
| `POST` | `/api/empleados` | Administrador o RecursosHumanos |
| `PUT` | `/api/empleados/{id}` | Administrador o RecursosHumanos |
| `DELETE` | `/api/empleados/{id}` | Administrador o RecursosHumanos |
| `GET` | `/api/departamentos` | Cualquier rol |

Filtros de empleados: `name`, `companyId`, `departmentId`, `status`, `type`,
`pageNumber`, `pageSize`.

### Cálculo de pagos semanales

| Método | Ruta | Descripción | Rol requerido |
|---|---|---|---|
| `GET` | `/api/nomina/vista-previa` | Calcula la semana sin guardarla e informa si ya fue pagada | Cualquier rol |
| `POST` | `/api/nomina/ejecuciones` | Genera el pago de la semana. **409 si ya existe** | Administrador o RecursosHumanos |
| `GET` | `/api/nomina/ejecuciones` | Historial paginado | Cualquier rol |
| `GET` | `/api/nomina/ejecuciones/{id}` | Detalle con el desglose por empleado | Cualquier rol |
| `POST` | `/api/nomina/ejecuciones/{id}/anular` | Anula y libera la semana | Administrador |
| `GET` | `/api/nomina/semanas-generadas` | Semanas ya pagadas de un año | Cualquier rol |

Parámetros de la vista previa: `companyId`, `year`, `weekNumber`,
`onlyActiveEmployees`. Filtros del historial: `companyId`, `year`,
`includeCancelled`, `pageNumber`, `pageSize`.

### Registro de eventos

| Método | Ruta | Rol requerido |
|---|---|---|
| `GET` | `/api/registro-eventos/archivos` | **Administrador** |
| `GET` | `/api/registro-eventos` | **Administrador** |

Filtros: `fileName`, `minimumLevel`, `searchTerm`, `maximumEntries`.
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

## 7. El flujo de pago semanal

Es el caso de uso central del sistema. Estos son los pasos, con lo que hace la
aplicación en cada uno.

### 7.1 En el portal web

1. **Registrar las compañías** (menú *Nómina → Compañías*). Cada compañía tiene
   razón social y Registro Nacional de Contribuyente único.
2. **Capturar los empleados** (menú *Nómina → Empleados*), asignando a cada uno su
   compañía, su departamento y su tipo de contrato. Los campos solicitados cambian
   según el tipo.
3. **Calcular el pago de la semana** (menú *Nómina → Calcular pago semanal*):
   se elige la compañía y la semana, y la pantalla muestra el cálculo propuesto con
   el desglose de cada empleado. **Nada se ha guardado todavía.**
4. **Generar el pago.** El documento queda almacenado con la instantánea de lo que
   se pagó. Si la semana ya fue pagada, el botón queda deshabilitado y se explica
   por qué.
5. **Consultar el historial** (menú *Nómina → Historial de pagos*): las nóminas de
   semanas anteriores, con el detalle de cada una.

### 7.2 Una semana solo se paga una vez

La regla se aplica en dos niveles independientes:

| Nivel | Mecanismo |
|---|---|
| Interfaz | La vista previa consulta si la semana ya fue pagada y deshabilita el botón antes de que el usuario pueda intentarlo. |
| Servicio | Comprueba la existencia de una ejecución vigente y responde **HTTP 409** con el identificador de la nómina existente. |
| Base de datos | Índice único filtrado sobre `(CompanyId, Year, WeekNumber)` para las ejecuciones vigentes. Cierra la ventana de una condición de carrera entre dos peticiones simultáneas. |

Si hay que corregir una semana ya pagada, un **administrador** la anula indicando
el motivo. El documento se conserva como evidencia y la semana queda libre para
recalcularse.

### 7.3 El historial no se recalcula

Cada línea de una nómina generada guarda el monto, la fórmula aplicada y el
desglose **tal como quedaron el día en que se generó**. Si mañana cambian las
horas trabajadas de un empleado, la nómina de la semana pasada sigue mostrando lo
que realmente se pagó. Recalcular el histórico con los datos vigentes sería un
error, no una optimización.

### 7.4 Datos de demostración

En el primer arranque se siembran **3 compañías**, **11 empleados** repartidos
entre ellas y **24 nóminas históricas** (las 8 semanas anteriores a la actual, por
cada compañía), con variación determinista de horas y ventas por semana para que
el historial se parezca a uno real. La semana en curso y la anterior se dejan sin
generar a propósito, para poder probar el flujo completo.

---

## 8. Ejemplo de uso de la API

Obtener el token:

```bash
curl -X POST http://localhost:5080/api/autenticacion/iniciar-sesion -H "Content-Type: application/json" -d "{\"userName\":\"administrador\",\"password\":\"Sb2024Admin\"}"
```

Consultar las compañías para obtener su identificador:

```bash
curl -H "Authorization: Bearer <TOKEN>" http://localhost:5080/api/companias
```

Ver el cálculo propuesto de una semana, sin guardarlo:

```bash
curl -H "Authorization: Bearer <TOKEN>" "http://localhost:5080/api/nomina/vista-previa?companyId=<GUID>&year=2026&weekNumber=34"
```

Generar el pago de esa semana:

```bash
curl -X POST http://localhost:5080/api/nomina/ejecuciones -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json" -d "{\"companyId\":\"<GUID>\",\"year\":2026,\"weekNumber\":34,\"onlyActiveEmployees\":true}"
```

Intentarlo de nuevo devuelve **HTTP 409** con el identificador de la nómina que ya
existe:

```json
{
  "title": "La semana ya tiene nomina generada.",
  "status": 409,
  "detail": "La compania 'Servicios Financieros del Caribe, S. A.' ya tiene la nomina de la semana 34 del ano 2026 generada. Una semana solo puede pagarse una vez; anule la ejecucion existente si necesita volver a calcularla.",
  "errorCode": "NOMINA_SEMANA_YA_GENERADA",
  "existingPayrollRunId": "fe49a02d-566e-4036-a8ea-8946c83b418e",
  "payrollYear": 2026,
  "payrollWeekNumber": 34
}
```

Registrar un empleado por horas (el pago se calcula como `300 × 40 + 300 × 1.5 × 5`):

```bash
curl -X POST http://localhost:5080/api/empleados -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json" -d "{\"type\":\"Hourly\",\"paternalLastName\":\"Diaz\",\"socialSecurityNumber\":\"001-9999999-9\",\"companyId\":\"<GUID>\",\"departmentId\":\"<GUID>\",\"status\":\"Active\",\"hourlyWage\":300,\"hoursWorked\":45}"
```

Respuesta (fragmento):

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

Consultar el registro de eventos (solo administrador):

```bash
curl -H "Authorization: Bearer <TOKEN>" "http://localhost:5080/api/registro-eventos?minimumLevel=Warning&maximumEntries=50"
```

## 9. Reglas de cálculo de nómina

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

## 10. Registro de eventos (logs)

Serilog escribe en tres destinos:

- **Consola**, en formato compacto para desarrollo.
- **Archivo de texto diario** en `Logs/sb-api-sb-AAAAMMDD.log`, pensado para leerse
  en una terminal.
- **Archivo JSON diario** en `Logs/sb-api-sb-AAAAMMDD.json`, pensado para
  procesarse. Ambos con rotación diaria, límite de 50 MB por archivo y retención de
  30 días.

### 10.1 Consulta desde la aplicación

El menú **Seguridad → Registro de eventos**, visible **solo para el rol
administrador**, permite consultar el log sin acceso al servidor de archivos:
seleccionar el archivo, filtrar por nivel mínimo, buscar en el mensaje o en el
identificador de correlación, y desplegar la traza completa de las entradas con
excepción.

La lectura se hace sobre el archivo **JSON**: analizar registro estructurado es
fiable, mientras que aplicar expresiones regulares al archivo de texto se rompe con
cualquier mensaje que contenga un salto de línea. Solo se leen las últimas líneas
del archivo, por lo que el consumo de memoria no depende de su tamaño.

Se registra: el arranque y la siembra de datos, una línea por petición HTTP con
método, ruta, código de respuesta y duración, cada operación de negocio (altas,
cambios, eliminaciones) con el usuario responsable, los intentos de
autenticación fallidos, las validaciones rechazadas y la traza completa de
cualquier error no controlado. Cada evento lleva un **identificador de
correlación** que también se devuelve al cliente en las respuestas de error.

---

## 11. Convenciones de código aplicadas

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

## 12. Documentación adicional

- [`docs/REPORTE-TECNICO.md`](docs/REPORTE-TECNICO.md) — arquitectura,
  metodologías, tecnologías empleadas y justificación de cada decisión.
- [`docs/RESPUESTAS-CONCEPTUALIZACION.md`](docs/RESPUESTAS-CONCEPTUALIZACION.md) —
  respuestas conceptuales redactadas en formato de correo.

---

## 13. Notas de entrega

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
