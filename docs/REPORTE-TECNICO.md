# Reporte técnico — SB.API_SB

Metodologías, tecnologías y decisiones de diseño de la solución, con el
razonamiento detrás de cada una.

---

## 1. Resumen

| Aspecto | Decisión |
|---|---|
| Lenguaje / framework | C# 12 sobre .NET 8 |
| Arquitectura backend | Onion Architecture (5 proyectos) |
| API | ASP.NET Core Web API, REST, controladores |
| Persistencia relacional | Entity Framework Core 8 (SQL Server / SQLite) |
| Persistencia de texto plano | Repositorio propio sobre archivo delimitado |
| Autenticación | JWT `Authorization: Bearer`, HMAC-SHA256 |
| Contraseñas | PBKDF2 (Rfc2898) + SHA-256, 100,000 iteraciones |
| Validaciones | FluentValidation con reglas condicionales por tipo |
| Registro de eventos | Serilog (consola + archivo diario) |
| Documentación | Swagger / OpenAPI con esquema de seguridad Bearer |
| Pruebas | xUnit + NSubstitute — 77 pruebas |
| Frontend | React 18 + TypeScript 5 + Vite 5 |
| Cliente HTTP | Axios con interceptores |

Volumen: **≈ 100 archivos de código**, 5 proyectos .NET, 1 proyecto de pruebas y
1 aplicación web. 181 entidades gubernamentales cargadas del listado oficial.

---

## 2. Arquitectura Onion

### 2.1 Las capas y su responsabilidad

```
                    ┌─────────────────────────────┐
                    │      Presentation           │  Controladores, middlewares,
                    │  (host de la API)           │  Swagger, composición de DI
                    └──────────────┬──────────────┘
                                   │
              ┌────────────────────┼────────────────────┐
              │                    │                    │
    ┌─────────▼─────────┐  ┌───────▼────────┐  ┌────────▼─────────┐
    │     Services      │  │ Infrastructure │  │   Application    │
    │ Casos de uso      │  │ EF Core, JWT,  │  │ DTO, contratos,  │
    │ (implementación)  │  │ archivo plano  │  │ validaciones     │
    └─────────┬─────────┘  └───────┬────────┘  └────────┬─────────┘
              │                    │                    │
              └────────────────────┼────────────────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │          Domain             │  Entidades, reglas de negocio,
                    │      (sin dependencias)     │  enums, excepciones, contratos
                    └─────────────────────────────┘
```

| Capa | Proyecto | Contiene | No contiene |
|---|---|---|---|
| Dominio | `SB.API_SB.Domain` | Entidades, jerarquía de empleados con sus fórmulas, enums, constantes de negocio, excepciones, interfaces de repositorio | Ninguna referencia a proyectos ni paquetes externos |
| Aplicación | `SB.API_SB.Application` | DTO de entrada y salida, interfaces de servicio, validadores, mapeos, abstracciones de reloj y seguridad | Implementaciones, SQL, HTTP |
| Servicios | `SB.API_SB.Services` | Implementación de los casos de uso, manejadores por tipo de empleado, generación del reporte | Sentencias SQL, acceso a archivos |
| Infraestructura | `SB.API_SB.Infrastructure` | `DbContext`, configuraciones de mapeo, repositorios, repositorio de texto plano, generador de JWT, hasher | Reglas de negocio |
| Presentación | `SB.API_SB.Presentation` | Controladores, middleware de excepciones, filtro de validación, Swagger, CORS, políticas de autorización | Reglas de negocio, acceso directo a datos |

### 2.2 Por qué Onion y no una arquitectura en capas tradicional

En una arquitectura en capas clásica, la capa de negocio depende de la capa de
datos: `Negocio → Datos`. Eso implica que el modelo de negocio se contamina con
el ORM y que no se puede probar sin base de datos.

Onion **invierte esa dependencia**. El Dominio declara *qué* necesita
(`IEmployeeRepository`) y la Infraestructura provee el *cómo*
(`EmployeeRepository` sobre EF Core). Las consecuencias medibles en este
proyecto:

1. **El proyecto de Dominio no tiene ni un solo `PackageReference`.** Se puede
   verificar abriendo `SB.API_SB.Domain.csproj`. Esa ausencia es la prueba de que
   la regla se respetó, no una promesa en un documento.
2. **Dos tecnologías de persistencia conviven sin fricción.** Las entidades
   gubernamentales viven en un archivo de texto y los empleados en una base
   relacional. Ningún servicio distingue una de otra porque ambas satisfacen
   contratos del Dominio.
3. **Las reglas de negocio se prueban sin infraestructura.** De las 77 pruebas,
   ninguna necesita una base de datos ni un servidor web (la única que toca disco
   es la del repositorio de texto plano, y por diseño: verifica precisamente ese
   viaje).
4. **Cambiar de SQL Server a SQLite es una línea de configuración.** Está
   demostrado: el proyecto se ejecuta con SQLite en desarrollo y está configurado
   para SQL Server en producción, con el mismo `DbContext` y los mismos mapeos.

### 2.3 Composición de dependencias

Cada capa expone su propio método de registro:

```csharp
builder.Services.AddInfrastructure(builder.Configuration);  // EF Core, archivo plano, JWT
builder.Services.AddApplicationServices();                   // servicios y validadores
builder.Services.AddPresentationServices(builder.Configuration); // controladores, CORS, Swagger
```

`Program.cs` es el único archivo que conoce todas las capas: es el *Composition
Root*. Agregar una implementación nueva no obliga a tocar el arranque, solo el
método de registro de su capa.

---

## 3. Diseño orientado a objetos y SOLID

### 3.1 El cálculo de nómina como jerarquía polimórfica

Es la decisión de diseño central del proyecto. La alternativa habitual —un
`switch` sobre el tipo de empleado— habría producido esto:

```csharp
// Enfoque descartado
decimal CalcularPago(Empleado empleado) => empleado.Tipo switch
{
    TipoEmpleado.Asalariado => empleado.SalarioSemanal,
    TipoEmpleado.PorHoras   => /* ... */,
    // Cada tipo nuevo obliga a modificar este método y a reprobar todo lo demás.
};
```

La solución implementada declara el cálculo como abstracto en la clase base y
cada subtipo aporta su fórmula:

```csharp
public abstract class Employee : AuditableEntity
{
    public abstract EmployeeType Type { get; }
    public abstract decimal CalculateWeeklyPayment();
    public abstract PaymentBreakdown BuildPaymentBreakdown();
}

public sealed class HourlyEmployee : Employee
{
    public override decimal CalculateWeeklyPayment() =>
        RoundCurrency(CalculateRegularPayment() + CalculateOvertimePayment());
}
```

`BaseSalariedCommissionEmployee` hereda de `CommissionEmployee` y **reutiliza su
cálculo de comisión** (`CalculateCommission`), en lugar de repetir la
multiplicación. La fórmula de comisión existe una sola vez en todo el sistema.

### 3.2 SOLID, con la evidencia en el código

| Principio | Dónde se aplica | Evidencia |
|---|---|---|
| **S** — Responsabilidad única | `PayrollReportService` solo agrega y totaliza; el cálculo lo hace cada empleado. `ExceptionHandlingMiddleware` solo traduce errores. | Ningún servicio contiene una fórmula de nómina. |
| **O** — Abierto/cerrado | Agregar un quinto tipo de contrato = nueva subclase + nuevo `IEmployeeTypeHandler` + una línea de registro. | Ni `EmployeeService`, ni `PayrollReportService`, ni los controladores cambian. |
| **L** — Sustitución de Liskov | Toda `Employee` responde a `CalculateWeeklyPayment()` con un contrato idéntico: monto no negativo, redondeado a dos decimales. | El reporte recorre la colección sin comprobar tipos. |
| **I** — Segregación de interfaces | `IEmployeeRepository`, `IUserRepository`, `IPasswordHasher`, `IJwtTokenGenerator`, `IDateTimeProvider` son pequeñas y específicas. | Ninguna clase implementa métodos que no usa. |
| **D** — Inversión de dependencias | Los contratos viven en Dominio/Aplicación; las implementaciones en Infraestructura. | El `.csproj` del Dominio no tiene dependencias. |

### 3.3 El patrón de manejadores por tipo

El problema práctico: un solo contrato de entrada (`EmployeeRequestBase`) debe
construir cuatro subtipos distintos y proyectar campos distintos en la respuesta.
Resolverlo con condicionales habría concentrado en `EmployeeService` el
conocimiento de los cuatro tipos.

En su lugar, cada tipo tiene un manejador:

```csharp
public interface IEmployeeTypeHandler
{
    EmployeeType HandledType { get; }
    string TypeDescription { get; }
    Employee CreateEmployee(EmployeeRequestBase request);
    void ApplyTypeSpecificValues(Employee employee, EmployeeRequestBase request);
    void ProjectTypeSpecificValues(Employee employee, EmployeeResponse response);
}
```

El contenedor inyecta todos los manejadores registrados y
`EmployeeTypeHandlerResolver` los indexa por tipo. `EmployeeService` pide el
manejador que le corresponde y delega. La prueba
`Resolve_TodosLosTiposDelEnumerado_TienenManejadorRegistrado` falla si alguien
agrega un valor al enumerado sin su manejador: el diseño se protege a sí mismo.

Este mismo patrón se replica en el frontend
(`frontend/src/constants/employeeTypes.ts`): el catálogo de tipos es un arreglo
de datos que alimenta el selector, los campos del formulario, el filtro y el
texto de la fórmula. Agregar un tipo en la interfaz es agregar una entrada al
arreglo.

---

## 4. La base de datos de texto plano

El requerimiento pide un archivo de texto plano dentro del proyecto. Un archivo
de texto no ofrece transacciones, ni control de concurrencia, ni integridad
referencial. La implementación compensa cada una de esas carencias de forma
explícita.

### 4.1 Formato

Archivo delimitado por barra vertical, con encabezado autodescriptivo:

```
# SB.API_SB - Base de datos de texto plano
# Campos: Id|Nombre|Categoria|PoderDelEstado|Sector|Estado|CreadoEnUtc|CreadoPor|ActualizadoEnUtc|ActualizadoPor
# Estado: 1 = Activo, 2 = Inactivo
# El caracter | dentro de un valor se almacena escapado como \p
ae945ce9-...|Acuario Nacional|Organismo Descentralizado Funcionalmente|Poder Ejecutivo|Medio Ambiente y Recursos Naturales|1|2026-08-26T19:46:35.9890677Z|Semilla||
```

### 4.2 Los cuatro problemas resueltos

| Problema del formato plano | Solución implementada |
|---|---|
| Un valor con `\|` rompe el formato | Escape reversible: `\` → `\\`, `\|` → `\p`, salto de línea → `\n`. Verificado en `FlatFileRecordSerializerTests` con seis casos límite. |
| Dos peticiones simultáneas se pisan | `SemaphoreSlim` serializa todo acceso; el repositorio es singleton para que la garantía cubra la aplicación entera. |
| Una interrupción deja el archivo a medio escribir | Escritura atómica: se escribe un `.tmp` completo y solo entonces se reemplaza el original. Además, copia de respaldo con estampa de tiempo antes de cada reescritura. |
| Leer el archivo en cada consulta es costoso | Cache en memoria invalidada al escribir. Ante un fallo de escritura la cache se descarta para que la siguiente lectura vuelva a la única fuente de verdad. |

Las lecturas devuelven **copias** de la cache, de modo que ningún consumidor
puede alterar el estado interno del repositorio por accidente.

### 4.3 Semilla versionada, datos generados no versionados

- `GovernmentEntities.seed.txt` (versionado): cuatro columnas extraídas del
  archivo Excel oficial. Legible y fácil de comparar en un *diff*.
- `GovernmentEntities.txt` (no versionado): se genera en el primer arranque con
  identificadores, estado y auditoría.

Así el repositorio se clona y ejecuta sin pasos manuales, y los datos capturados
durante las pruebas de un desarrollador no entran al control de versiones.

---

## 5. Persistencia relacional con Entity Framework Core

### 5.1 Estrategia de herencia: Table Per Hierarchy

Los cuatro subtipos de empleado se mapean a una única tabla `Employees` con la
columna discriminadora `EmployeeType`.

**Motivo:** el reporte semanal recorre todos los tipos a la vez. Con TPH eso es
una sola consulta sin uniones. Con Table Per Type serían cuatro `LEFT JOIN` para
la misma información. El costo de TPH —columnas nulas para los tipos que no las
usan— es intrascendente en un volumen de miles de empleados.

### 5.2 Consultas: el filtrado ocurre en el motor

`EmployeeRepository.SearchAsync` construye el `IQueryable` con los filtros y
aplica `CountAsync` + `Skip`/`Take` antes de materializar. **Nunca se trae la
tabla completa a memoria para filtrarla en C#.** El filtro por tipo se traduce
consultando la propiedad sombra del discriminador:

```csharp
query = query.Where(employee =>
    EF.Property<int>(employee, EmployeeConfiguration.DISCRIMINATOR_COLUMN_NAME) == discriminatorValue);
```

Las consultas de solo lectura usan `AsNoTracking()`: sin seguimiento de cambios,
menos memoria y menos trabajo del *change tracker*.

### 5.3 Índices y relaciones

| Índice | Propósito |
|---|---|
| `IX_Employees_SocialSecurityNumber` (único) | Integridad: la unicidad se garantiza en la base de datos, no solo en una validación que una condición de carrera podría burlar. |
| `IX_Employees_PaternalLastName` | Respalda el filtro por nombre. |
| `IX_Employees_DepartmentId_Status` | Índice compuesto que respalda el filtro combinado más frecuente. |
| `IX_Departments_Code`, `IX_Users_UserName`, `IX_Users_Email`, `IX_Roles_Name` (únicos) | Unicidad de códigos y credenciales. |

Relaciones: `Employee → Department` (muchos a uno, con `DeleteBehavior.Restrict`
para que no se borre un departamento con empleados) y `User ↔ Role` (muchos a
muchos mediante la entidad explícita `UserRole`, que además audita la fecha de
asignación).

### 5.4 Auditoría automática

`ApplicationDbContext.SaveChangesAsync` recorre el *change tracker* y completa
`CreatedAt`, `CreatedBy`, `UpdatedAt` y `UpdatedBy` según el estado de cada
entidad. Centralizarlo ahí hace **imposible** que un servicio se olvide de
registrar la auditoría.

### 5.5 Fechas siempre en UTC

Los proveedores relacionales devuelven `DateTime` con
`DateTimeKind.Unspecified`. Al serializarlas a JSON quedan sin sufijo de zona y
el cliente las interpreta como hora local, mostrando un desfase igual al de su
zona horaria. **Este problema se detectó durante las pruebas del portal** (el
último acceso aparecía cuatro horas adelantado) y se corrigió con un
`ValueConverter` aplicado por reflexión a todas las propiedades de fecha del
modelo:

```csharp
foreach (var property in entityType.GetProperties())
{
    if (property.ClrType == typeof(DateTime)) property.SetValueConverter(utcConverter);
    else if (property.ClrType == typeof(DateTime?)) property.SetValueConverter(nullableUtcConverter);
}
```

Se resuelve una vez en el modelo, no en cada consulta ni en cada pantalla, y toda
entidad futura queda cubierta automáticamente.

---

## 6. Seguridad

### 6.1 Autenticación JWT

El token se firma con HMAC-SHA256 y transporta el identificador del usuario, su
nombre, su correo y un *claim* de rol por cada rol asignado. Eso permite que la
API autorice por rol **sin volver a consultar la base de datos en cada
petición**.

Las cuatro validaciones están activas de forma explícita: emisor, audiencia,
vigencia y firma. Dejar alguna en `false` convertiría el token en un dato de
entrada no confiable.

`JwtTokenGenerator` valida su configuración **una sola vez, al construirse**: si
la clave de firma falta o tiene menos de 32 caracteres, la aplicación falla al
arrancar en lugar de emitir tokens débiles en silencio.

### 6.2 Contraseñas

PBKDF2 (`Rfc2898DeriveBytes`) con SHA-256, sal aleatoria de 16 bytes por usuario
y 100,000 iteraciones. Elegido porque forma parte de la biblioteca base de .NET
—sin dependencias externas— y porque es lento por diseño, lo que encarece los
ataques de diccionario.

Dos detalles que importan:

- La comparación final usa `CryptographicOperations.FixedTimeEquals`, que no
  filtra información por el tiempo de respuesta.
- Un hash o una sal corruptos devuelven `false` en lugar de lanzar una excepción:
  un dato dañado no debe tumbar la autenticación.

### 6.3 Autorización por rol

Tres roles y tres políticas:

| Política | Roles admitidos |
|---|---|
| `SoloAdministracion` | Administrador |
| `EscrituraMantenimiento` | Administrador, RecursosHumanos |
| `LecturaMantenimiento` | Administrador, RecursosHumanos, Consultor |

La **política de reserva** (`FallbackPolicy`) exige usuario autenticado. Un
endpoint nuevo queda protegido por omisión; abrirlo requiere marcarlo
explícitamente con `[AllowAnonymous]`. Es la diferencia entre "seguro salvo que
alguien se acuerde" y "seguro salvo que alguien lo decida".

### 6.4 Otras medidas

- **CORS restringido** a los orígenes declarados en configuración, nunca
  `AllowAnyOrigin`.
- **Mensajes de error genéricos** en la autenticación: no se distingue entre
  usuario inexistente, contraseña incorrecta y usuario inactivo, para no permitir
  enumerar usuarios válidos. El log interno sí registra el motivo real.
- **DTO de salida sin datos sensibles**: `UserResponse` no expone el hash ni la
  sal. Ese es justamente el motivo de que exista el DTO.
- **Un usuario no puede eliminar su propia cuenta.**
- **Sin secretos en el código**: cadenas de conexión y clave de firma viven en
  `appsettings.json`, con `UserSecretsId` configurado para desarrollo.
- **El frontend oculta lo que el usuario no puede usar, pero la autorización real
  la aplica la API.** Ocultar un botón es comodidad, no seguridad.

---

## 7. Validaciones

### 7.1 Validaciones condicionales por tipo

El reto: un único contrato de entrada para cuatro tipos con campos obligatorios
distintos. FluentValidation lo resuelve con reglas condicionales agrupadas en
métodos privados con nombre:

```csharp
private void ValidateHourlyEmployee()
{
    When(request => request.Type == EmployeeType.Hourly, () =>
    {
        RuleFor(request => request.HourlyWage)
            .NotNull().WithMessage("El sueldo por hora es obligatorio para el empleado por horas.")
            .GreaterThan(0m).WithMessage("El sueldo por hora debe ser mayor que cero.");
        // ...
    });
}
```

Nueve pruebas fijan este comportamiento, incluida la que verifica que **el primer
nombre no es obligatorio para el empleado por horas** —lectura fiel de la
especificación, que solo lo solicita para los otros tres tipos.

### 7.2 Aplicación transversal

Un `IAsyncActionFilter` (`RequestValidationFilter`) busca en el contenedor un
`IValidator<T>` para cada argumento de la acción y lo ejecuta. Registrado
globalmente, **ninguna acción puede olvidarse de validar su entrada**. Los
controladores quedan sin una sola línea de código de validación.

### 7.3 Las tres barreras

| Barrera | Qué valida | Ejemplo |
|---|---|---|
| Frontend | Formato y obligatoriedad, para dar retroalimentación inmediata | Campo requerido, rango numérico |
| API (FluentValidation) | Reglas del contrato | Tarifa de comisión entre 0 y 1 |
| API (servicio) | Reglas que requieren consultar datos | Seguro social duplicado, departamento inactivo |
| Base de datos | Integridad final | Índice único, clave foránea |

La validación del cliente es comodidad; la de la API es la que decide.

---

## 8. Manejo de excepciones

Un middleware traduce cada excepción a `ProblemDetails` (RFC 7807):

| Excepción | HTTP | Código de error |
|---|---|---|
| `ValidationException` | 400 | `VALIDACION_FALLIDA` (+ errores por campo) |
| `BusinessRuleViolationException` | 400 | `REGLA_DE_NEGOCIO_INCUMPLIDA` |
| `InvalidCredentialsException` | 401 | `CREDENCIALES_INVALIDAS` |
| `EntityNotFoundException` | 404 | `ENTIDAD_NO_ENCONTRADA` |
| `DuplicatedEntityException` | 409 | `REGISTRO_DUPLICADO` |
| Cualquier otra | 500 | `ERROR_INTERNO` |

Tres decisiones dentro del middleware:

1. **Las excepciones previsibles se registran como advertencia; las inesperadas,
   como error con traza completa.** Un 404 no es un incidente; un
   `NullReferenceException` sí.
2. **El error 500 nunca devuelve la traza al cliente.** Devuelve un
   identificador de correlación que también aparece en el log, de modo que el
   usuario puede reportarlo y el equipo puede encontrarlo.
3. **Se verifica `Response.HasStarted`** antes de escribir: si la respuesta ya
   comenzó, se registra la situación en lugar de lanzar una segunda excepción.

Concentrarlo aquí elimina los bloques `try/catch` repetidos de los controladores
y garantiza que **toda** respuesta de error tenga el mismo formato.

---

## 9. Registro de eventos con Serilog

Configurado por completo desde `appsettings.json`, de modo que los niveles y
destinos se cambian sin recompilar. Dos destinos: consola y archivo diario con
rotación, límite de 50 MB y retención de 30 días.

**Registro estructurado.** No se concatenan cadenas:

```csharp
logger.LogInformation(
    "Empleado {EmployeeId} creado. Tipo: {EmployeeType}. Pago semanal: {WeeklyPayment}.",
    employee.Id, employee.Type, employee.CalculateWeeklyPayment());
```

Cada propiedad queda consultable de forma independiente, lo que permite buscar
"todas las operaciones sobre el empleado X" sin depender de expresiones
regulares sobre el texto.

`UseSerilogRequestLogging` emite **una línea por petición** —método, ruta, código
y duración— en lugar de las múltiples del registro predeterminado, y ajusta el
nivel según el resultado: 5xx como error, 4xx como advertencia, el resto como
información. Cada línea se enriquece con el identificador de correlación, el
usuario y la dirección del cliente.

---

## 10. Documentación con Swagger

Además del listado de endpoints:

- **Esquema de seguridad Bearer declarado**, de modo que se puede autenticar y
  probar la API completa desde la propia página de Swagger.
- **Comentarios XML incluidos** de las capas de Presentación y Aplicación: cada
  operación y cada propiedad de los contratos llega documentada al navegador. El
  mismo comentario que lee quien mantiene el código es el que ve quien consume la
  API.
- **Códigos de respuesta declarados** con `[ProducesResponseType]` en cada
  acción, incluidos los de error.

---

## 11. Pruebas

77 pruebas unitarias con **xUnit** y **NSubstitute**.

### 11.1 Qué se probó y por qué

| Área | Motivo |
|---|---|
| Las cuatro fórmulas de nómina | Es la regla más crítica: un error se traduce en dinero mal pagado. |
| El límite de las 40 horas | Se prueba con 0, 20, 40, 41, 46 y 50 horas. El caso de exactamente 40 es donde un `<` en lugar de un `<=` pasaría desapercibido. |
| Suma del desglose | Los componentes del reporte deben sumar exactamente el total. Un desglose que no cuadra es peor que no tener desglose. |
| Escape del archivo plano | Seis casos límite, incluido un valor que contiene la propia secuencia de escape. |
| Rendimiento con 1,000 empleados | Verifica el requisito no funcional de forma automática, no por inspección. |
| Reglas del servicio | Duplicados, departamento inactivo, cambio de tipo prohibido: se prueban con dobles, sin base de datos. |

### 11.2 Testabilidad como consecuencia del diseño

Dos abstracciones hacen las pruebas deterministas:

- `IDateTimeProvider` — `FixedDateTimeProvider` devuelve siempre la misma fecha,
  por lo que se puede afirmar `Assert.Equal(fechaEsperada, report.GeneratedAtUtc)`.
- `IFlatFilePathResolver` — el repositorio de texto plano apunta a un directorio
  temporal que cada prueba crea y elimina.

Sin esas abstracciones habría que probar contra el reloj del sistema y contra el
directorio del proyecto: pruebas frágiles y con efectos secundarios.

---

## 12. Frontend: React + TypeScript

### 12.1 Decisiones y su justificación

| Decisión | Justificación |
|---|---|
| **Vite** en lugar de Create React App | CRA está descontinuado. Vite ofrece arranque instantáneo y recarga en caliente. |
| **TypeScript estricto** (`strict`, `noUnusedLocals`, `noImplicitReturns`) | Los contratos de la API se declaran una vez en `types/api.ts`; si un campo cambia, el compilador señala cada lugar del cliente que hay que ajustar. |
| **Sin librería de componentes** (Material UI, Bootstrap) | La maqueta define una identidad visual concreta. Partir de una librería habría significado sobrescribir sus estilos. El CSS propio son 12 KB con la paleta institucional como única fuente. |
| **Sin librería de iconos** | Nueve iconos SVG en línea que heredan `currentColor`. Cero peso adicional. |
| **Sin librería de formularios** | Tres formularios con reglas distintas. Estado controlado y validación explícita resultan más legibles que la configuración de una librería. |
| **Sin librería de estado global** (Redux, Zustand) | El único estado global real es la sesión. Un `Context` lo resuelve; Redux sería infraestructura sin problema que resolver. |
| **Axios con interceptores** | Un interceptor agrega el token a cada petición y otro traduce los errores y avisa cuando la sesión caduca. Ninguna pantalla manipula encabezados de autorización. |

Dependencias de producción: **4** (react, react-dom, react-router-dom, axios).

### 12.2 Fidelidad a la maqueta

| Elemento de la maqueta | Implementación |
|---|---|
| Azul `rgba(13, 48, 72, .9)` | Variable CSS `--color-blue`, verificada en el navegador |
| Gris `rgba(237, 240, 247)` | Variable CSS `--color-gray`, fondo del panel de contenido |
| Barra lateral azul con logotipo | `AppLayout` + `BrandLogo` (SVG en línea) |
| Menú Inicio / Consulta / Crear registro | Rutas `/inicio`, `/entidades`, `/entidades/nuevo` |
| Icono naranja en el elemento activo | `.sidebar__link--active .sidebar__link-icon` con `--color-accent` |
| Título de la página en el encabezado | `app-main__title`, con título y subtítulo por ruta |
| Panel gris con esquinas redondeadas | `.app-panel`, `border-radius: 20px` |
| Tarjetas blancas sobre el panel | `.card` |

Se añadieron iconos a todos los elementos del menú (la maqueta solo los muestra
en el elemento activo), por consistencia visual y para facilitar el
reconocimiento de cada sección.

### 12.3 La lógica de negocio no se duplica en el cliente

El formulario de empleados **no calcula el pago semanal**. Muestra la fórmula
como texto y el monto que devuelve la API. Duplicar la fórmula en TypeScript
crearía dos versiones de la misma regla que podrían discrepar tras un cambio
normativo. Lo mismo aplica a los catálogos de categoría y sector: se derivan de
los datos realmente almacenados, no de una lista fija en el cliente.

### 12.4 Prácticas de React aplicadas

- **Hooks propios**: `useAsyncData` encapsula el patrón cargar/cargando/error y
  descarta el resultado si el componente ya se desmontó.
- **`useCallback` y `useMemo`** donde evitan recálculos y renderizados
  innecesarios, no por costumbre.
- **Contexto en un módulo separado del proveedor**, para que la recarga en
  caliente de Vite funcione sin reiniciar el estado.
- **Filtros aplicados al presionar "Buscar"**, no en cada pulsación de tecla: una
  consulta por letra generaría peticiones que el usuario nunca llega a leer.
- **Confirmación explícita** antes de toda eliminación.
- **Accesibilidad**: `label` asociado a cada control, `aria-label` en los botones
  de icono, `role="dialog"` y cierre con Escape en las ventanas modales.
- **Paginación en el servidor**: el navegador nunca descarga las 181 entidades
  para mostrar 10.

---

## 13. Rendimiento

| Requisito | Cumplimiento |
|---|---|
| 1,000 empleados en menos de 2 s | Verificado por prueba automatizada. Una sola consulta con `AsNoTracking` y el cálculo en memoria: el tiempo medido es de milisegundos. |
| Consultas eficientes | Filtrado y paginación en el motor de base de datos, índices que respaldan cada filtro expuesto, `AsNoTracking` en lectura. |
| Archivo plano sin releer en cada consulta | Cache en memoria invalidada al escribir. |
| Transferencia mínima | El listado de empleados omite el desglose del cálculo; se solicita solo al abrir el detalle. |

---

## 14. Mantenibilidad y escalabilidad

### 14.1 Cómo se agrega un quinto tipo de empleado

Ejercicio concreto de la escalabilidad exigida:

| Paso | Archivo | Naturaleza |
|---|---|---|
| 1 | `Domain/Enums/EmployeeType.cs` | Un valor nuevo |
| 2 | `Domain/Entities/<NuevoTipo>Employee.cs` | Clase nueva con su fórmula |
| 3 | `Services/Employees/<NuevoTipo>EmployeeTypeHandler.cs` | Clase nueva |
| 4 | `Infrastructure/Persistence/Configurations/` | Configuración de sus columnas |
| 5 | `Services/ServicesServiceRegistration.cs` | Una línea de registro |
| 6 | `Application/Validators/Employees/EmployeeRequestBaseValidator.cs` | Un método `When` |
| 7 | `frontend/src/constants/employeeTypes.ts` | Una entrada en el arreglo |

**No se modifica** ningún servicio, controlador, repositorio ni componente
existente. Eso es el Principio Abierto/Cerrado medido en archivos tocados.

### 14.2 Eliminación de números mágicos

| Constante | Ubicación |
|---|---|
| Jornada semanal (40 h), recargo (1.5), incentivo (10%), decimales (2) | `PayrollConstants` |
| Longitudes y rangos de validación | `ValidationLimits` |
| Precisión y escala de columnas | `ColumnDefinitions` |
| Tamaño de sal, longitud de hash, iteraciones | `PasswordHasher` |
| Nombres de roles | `RoleNames` |
| Nombres de políticas de autorización | `AuthorizationPolicies` |

Si la normativa laboral cambia el recargo de horas extras, se edita una línea en
`PayrollConstants`.

### 14.3 Mapeo manual en lugar de AutoMapper

Los mapeos entre entidades y DTO se escriben como métodos de extensión
explícitos. Razones:

1. **Sin reflexión en tiempo de ejecución**: el mapeo es código compilado.
2. **Renombrar una propiedad produce un error de compilación**, no un campo nulo
   en producción.
3. **Sin dependencia adicional**, en un contexto donde AutoMapper cambió su
   licencia a comercial a partir de la versión 15.

El costo son unas líneas más por DTO. El beneficio es que el mapeo es visible y
verificable.

---

## 15. Cumplimiento de los requerimientos

| Requerimiento | Estado | Dónde |
|---|---|---|
| Lenguaje C# | ✅ | Toda la solución |
| Framework .NET 8 | ✅ | `Directory.Build.props`: `net8.0` |
| Nombre `[SB].[API_SB].[Capa]` | ✅ | Los cinco proyectos |
| Arquitectura Onion (4 capas + host) | ✅ | Domain, Application, Services, Infrastructure, Presentation |
| Autenticación Bearer JWT | ✅ | `JwtTokenGenerator`, `AuthenticationConfiguration` |
| Base de datos de texto plano en el proyecto | ✅ | `src/SB.API_SB.Presentation/Database/` |
| Mantenimiento de entidades gubernamentales | ✅ | 181 registros del listado oficial, CRUD completo |
| Manejo de logs (Serilog) | ✅ | Consola + archivo diario |
| Documentación (Swagger) | ✅ | `/swagger`, con esquema Bearer |
| Manejo de excepciones | ✅ | Middleware con `ProblemDetails` |
| Entrega en repositorio Git | ✅ | Repositorio con `.gitignore` |
| **Nomenclatura (11 reglas)** | ✅ | Tabla de verificación en el README, sección 10 |
| Gestión de empleados | ✅ | CRUD completo desde la interfaz |
| Filtros por nombre, departamento y estado | ✅ | Resueltos en la base de datos |
| Gestión de usuarios con roles | ✅ | Tres roles, muchos a muchos, JWT |
| Cálculo de pago por los cuatro tipos | ✅ | Polimorfismo en el Dominio |
| Recálculo al actualizar | ✅ | `EmployeeService.UpdateAsync` |
| Reporte semanal con detalle del cálculo | ✅ | Fórmula y componentes por empleado |
| Backend .NET 8 + EF Core | ✅ | EF Core 8, TPH, índices, relaciones |
| Frontend React + TypeScript | ✅ | React 18, TypeScript estricto, Vite |
| Base de datos SQL Server u Oracle | ✅ | SQL Server configurado; SQLite para ejecución inmediata |
| Maqueta y colores institucionales | ✅ | Verificado en el navegador |
| Validaciones de datos | ✅ | Cuatro barreras |
| Pruebas unitarias (mínimo 2–3) | ✅ | **77** |
| README con instrucciones | ✅ | Este repositorio |
| La aplicación loguea todo | ✅ | Peticiones, operaciones, errores, autenticación |

---

## 16. Verificación realizada

La solución no se entrega solo compilada: se ejecutó de extremo a extremo.

| Verificación | Resultado |
|---|---|
| `dotnet build` de la solución | Sin errores ni advertencias |
| `dotnet test` | 77/77 pruebas aprobadas |
| `npm run typecheck` y `npm run lint` | Sin errores ni advertencias |
| `npm run build` | 108 módulos, 261 KB (82 KB comprimido) |
| Inicio de sesión desde el portal | Token emitido, sesión establecida |
| Carga del listado oficial | 181 entidades, acentos correctos |
| Alta de entidad desde la interfaz | Persistida en el archivo plano con el usuario responsable |
| Nombre con `\|` en el dato | Almacenado como `A\pB`, leído de vuelta como `A\|B` |
| Nombre duplicado | HTTP 409 con el formato de error esperado |
| Eliminación de entidad | HTTP 204, registro eliminado, respaldo generado |
| Alta de empleado desde la interfaz | Formulario adaptado al tipo; pago calculado correctamente |
| Recálculo al editar horas | 45 h → RD$14,250; 50 h → RD$16,500 |
| Reporte semanal | Totales y desglose verificados a mano |
| Petición sin token / con token inválido | HTTP 401 |
| Validación por tipo | HTTP 400 con los errores del campo correspondiente |
| Colores institucionales en el navegador | `rgba(13, 48, 72, 0.9)` y `rgb(237, 240, 247)` |

**Un defecto encontrado y corregido durante esta verificación:** las fechas leídas
de la base de datos llegaban al cliente sin marca de zona y se mostraban con el
desfase de la zona horaria local. Se corrigió con un `ValueConverter` de UTC
aplicado a todo el modelo (sección 5.5).

---

## 17. Decisiones que merecen comentario

**1. Dos almacenes de datos.** El documento pide, en secciones distintas, un
archivo de texto plano y SQL Server u Oracle. Se cumplieron ambas exigencias
asignando a cada almacén el módulo que le corresponde. La arquitectura Onion es
justamente lo que hace que esa convivencia no genere complejidad accidental.

**2. SQLite como proveedor por omisión en desarrollo.** El requisito pide SQL
Server u Oracle. La solución está configurada para SQL Server, pero trae SQLite
como opción para que el evaluador pueda clonar y ejecutar sin instalar un motor
de base de datos. Es una línea de configuración; el `DbContext` y los mapeos son
los mismos.

**3. `EnsureCreated` en lugar de migraciones.** Para una prueba técnica, crear el
esquema al arrancar simplifica la evaluación. En producción el camino correcto
son las migraciones de EF Core (`dotnet ef migrations add`); el flag
`Database:ApplyAutomaticInitialization` permite desactivar la creación automática
cuando se adopten.

**4. El primer nombre es opcional para el empleado por horas.** Lectura fiel de
la especificación: en la captura del empleado por horas solo se solicitan
`apellidoPaterno`, `numeroSeguroSocial`, `sueldoPorHora` y `horasTrabajadas`. Se
implementó tal cual, con una prueba que lo documenta. Si fue una omisión del
documento, el cambio consiste en quitar una condición del validador.

**5. El logotipo se dibuja como SVG.** El requerimiento indica tomarlo del portal
de la Superintendencia. Para que la aplicación no dependa de descargar un recurso
externo, se dibujó una reproducción en SVG en línea, aislada en un único
componente (`BrandLogo.tsx`) que se sustituye por el archivo oficial sin tocar
nada más.

**6. `RollForward` en los proyectos ejecutables.** Los proyectos se compilan
contra .NET 8, como exige el requerimiento. `<RollForward>LatestMajor</RollForward>`
permite además ejecutarlos en máquinas donde solo esté instalado un runtime
mayor, sin cambiar el `TargetFramework`.

---

## 18. Qué añadiría en un proyecto de producción

Fuera del alcance de una prueba técnica, pero parte de una respuesta honesta:

| Mejora | Motivo |
|---|---|
| Migraciones de EF Core | Control de versiones del esquema y despliegues reproducibles. |
| *Refresh tokens* | Sesiones largas sin ampliar la vigencia del token de acceso. |
| Pruebas de integración con `WebApplicationFactory` | Verificar la tubería HTTP completa, no solo las unidades. |
| Bloqueo tras N intentos fallidos | Mitigar ataques de fuerza bruta. |
| Paginación por cursor | Si el volumen creciera al orden de millones de registros. |
| Registro centralizado (Seq, Elastic, Application Insights) | Consultar los logs de varias instancias en un solo lugar. |
| `HealthChecks` con verificación de dependencias | Comprobar base de datos y archivo, no solo que el proceso responde. |
| Historial de nómina | Hoy el pago se calcula sobre los datos vigentes; un historial permitiría reimprimir la nómina de una semana pasada. |
| Exportación a Excel y PDF del reporte | Necesidad habitual del área de recursos humanos. |
| Canal seguro obligatorio (HTTPS + HSTS) | Requisito de producción. |
