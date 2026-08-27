# Respuestas de conceptualización — formato de correo

> **Nota para quien envía este correo.** El documento de la prueba indica que
> «las respuestas de preguntas de conceptualización deben ser respondidas en un
> correo», pero el listado específico de preguntas no venía incluido en los
> archivos entregados (`ListaEntidadesGubernamentales.xlsx` y `Maqueta.jpeg`).
> Este documento responde los conceptos que la propia prueba menciona
> explícitamente —API, JWT, arquitectura Onion, SOLID, ORM, manejo de
> excepciones, logs, escalabilidad, mantenibilidad y testabilidad—. Si existe un
> cuestionario formal, indíquelo y se ajustan las respuestas a sus preguntas
> exactas.
>
> El texto de abajo está listo para copiarse y enviarse.

---

**Para:** [destinatario]@sb.gob.do
**CC:** [líder técnico]
**Asunto:** Prueba técnica — Respuestas de conceptualización y entrega de la solución SB.API_SB

---

Buenos días,

Adjunto el enlace al repositorio con la solución de la prueba técnica y, a
continuación, las respuestas a las preguntas de conceptualización.

La solución incluye la API RESTful en .NET 8 con arquitectura Onion, el portal
web en React con TypeScript, la consulta de las 181 entidades gubernamentales
sobre archivo de texto plano, el módulo de empleados —asociados a las entidades
de ese listado— con cálculo de nómina para los cuatro tipos de contrato, el
cálculo de pagos semanales por entidad con su historial, la gestión de usuarios
con autenticación JWT y 107 pruebas unitarias. El archivo `README.md` contiene
las instrucciones de ejecución y `docs/REPORTE-TECNICO.md` el detalle de las
decisiones de diseño.

---

## 1. ¿Qué es una API y qué la hace RESTful?

Una **API** (*Application Programming Interface*) es un contrato que expone las
capacidades de un sistema para que otros programas las consuman, sin conocer su
implementación interna.

Una API es **RESTful** cuando respeta los principios del estilo arquitectónico
REST:

- **Recursos identificados por URI.** `/api/empleados/{id}` identifica un
  empleado, no una acción.
- **Los verbos HTTP expresan la intención.** `GET` consulta, `POST` crea, `PUT`
  reemplaza, `DELETE` elimina. En nuestra API no existe `/api/crearEmpleado`: el
  verbo ya lo dice.
- **Sin estado (*stateless*).** Cada petición trae toda la información necesaria,
  incluido el token. El servidor no guarda sesión, lo que permite escalar
  horizontalmente sin sesiones compartidas.
- **Códigos de estado semánticos.** 200 correcto, 201 creado, 204 sin contenido,
  400 datos inválidos, 401 no autenticado, 403 sin permiso, 404 no encontrado,
  409 conflicto.
- **Representación uniforme.** JSON en toda la API, con el mismo formato de error
  (`ProblemDetails`, RFC 7807) en todos los casos.

## 2. ¿Qué es JWT y por qué usarlo?

Un **JWT** (*JSON Web Token*) es una cadena firmada digitalmente con tres partes
separadas por puntos: encabezado, contenido (*claims*) y firma.

En nuestra implementación el token transporta el identificador del usuario, su
nombre, su correo y un *claim* de rol por cada rol asignado, y está firmado con
HMAC-SHA256 usando una clave que vive en la configuración, nunca en el código.

**Por qué es la opción adecuada aquí:**

1. **Es autocontenido.** La API valida el token y lee los roles sin consultar la
   base de datos en cada petición.
2. **Es apátrida.** No requiere almacenar sesiones, lo que permite ejecutar
   varias instancias de la API detrás de un balanceador sin configuración
   adicional.
3. **Es estándar.** Cualquier cliente —React, una aplicación móvil, otro
   servicio— lo consume sin adaptaciones.

**Precauciones que aplicamos:** las cuatro validaciones activas (emisor,
audiencia, vigencia y firma); vigencia limitada a 120 minutos; la clave se valida
al arrancar y la aplicación falla si es débil o falta; y el token nunca contiene
datos sensibles, porque su contenido es legible por cualquiera —está firmado, no
cifrado—.

**Una limitación honesta:** un JWT no se puede revocar antes de que expire. Si se
requiere revocación inmediata, hace falta una lista de tokens invalidados o
*refresh tokens* de vigencia corta.

## 3. ¿Qué es la arquitectura Onion y qué ventaja ofrece?

Es una arquitectura organizada en capas concéntricas donde **todas las
dependencias apuntan hacia el centro**. En el centro está el Dominio —las reglas
de negocio— y en las capas externas los detalles técnicos: base de datos, HTTP,
archivos.

La diferencia con una arquitectura en capas tradicional es la dirección de las
dependencias. En el modelo clásico, `Negocio → Datos`: el negocio depende del
ORM. En Onion, el Dominio declara *qué* necesita mediante interfaces
(`IEmployeeRepository`) y la Infraestructura provee el *cómo*. La dependencia
queda invertida.

**Ventajas comprobables en esta solución:**

1. **El proyecto de Dominio no tiene ni una sola referencia externa.** Se puede
   verificar abriendo su archivo `.csproj`.
2. **Dos tecnologías de persistencia conviven sin fricción.** Las entidades
   gubernamentales están en un archivo de texto y los empleados en una base
   relacional; ningún servicio distingue una de otra.
3. **Las reglas de negocio se prueban sin infraestructura.** 76 de las 77 pruebas
   no necesitan base de datos ni servidor web.
4. **Cambiar de motor de base de datos es una línea de configuración.** Está
   demostrado: la solución se ejecuta con SQLite en desarrollo y está configurada
   para SQL Server, con el mismo `DbContext`.

## 4. ¿Qué son los principios SOLID y cómo se aplicaron?

Cinco principios de diseño orientado a objetos que reducen el costo de cambiar el
software.

- **S — Responsabilidad única.** Cada clase tiene un motivo para cambiar. El
  servicio de nómina agrega y totaliza; el cálculo lo hace cada empleado. Ningún
  servicio contiene una fórmula de pago.
- **O — Abierto/cerrado.** Abierto a extensión, cerrado a modificación. Agregar
  un quinto tipo de empleado son cuatro archivos nuevos y dos líneas de registro:
  no se modifica ningún servicio, controlador ni componente existente.
- **L — Sustitución de Liskov.** Cualquier subtipo puede reemplazar a su clase
  base. Toda `Employee` responde a `CalculateWeeklyPayment()` con el mismo
  contrato, por lo que el reporte recorre la colección sin comprobar tipos.
- **I — Segregación de interfaces.** Interfaces pequeñas y específicas
  (`IPasswordHasher`, `IJwtTokenGenerator`, `IDateTimeProvider`) en lugar de una
  interfaz general. Ninguna clase implementa métodos que no usa.
- **D — Inversión de dependencias.** Se depende de abstracciones, no de
  implementaciones. Es el principio que sostiene toda la arquitectura Onion.

El caso más ilustrativo es el cálculo de nómina: en lugar de un `switch` sobre el
tipo de empleado —que habría que modificar y reprobar cada vez que se agrega un
tipo—, la clase base declara el cálculo como abstracto y cada subtipo aporta su
fórmula. El comportamiento se resuelve por polimorfismo.

## 5. ¿Qué es un ORM y qué ventajas y riesgos tiene?

Un **ORM** (*Object-Relational Mapper*) traduce entre las tablas de una base de
datos relacional y los objetos del lenguaje. Usamos **Entity Framework Core 8**.

**Ventajas:** elimina el código repetitivo de mapeo; las consultas se escriben en
LINQ con verificación del compilador; parametriza automáticamente, lo que previene
inyección de SQL; el seguimiento de cambios permite agrupar operaciones en una
transacción; y abstrae el proveedor, lo que nos permitió soportar SQL Server y
SQLite con el mismo código.

**Riesgos y cómo los manejamos:**

| Riesgo | Mitigación aplicada |
|---|---|
| Consultas ineficientes generadas sin advertirlo | Los filtros y la paginación se construyen sobre `IQueryable` y viajan al motor; nunca se materializa la tabla completa. |
| El problema de las N+1 consultas | Uso explícito de `Include` donde se necesitan datos relacionados. |
| Sobrecarga del seguimiento de cambios | `AsNoTracking()` en todas las consultas de solo lectura. |
| Perder de vista el SQL real | El registro de EF Core queda activo en desarrollo, con las sentencias visibles en el log. |

Un ORM no exime de conocer bases de datos relacionales; los índices, las
relaciones y el plan de ejecución siguen siendo responsabilidad del
desarrollador.

## 6. ¿Cómo debe manejarse el manejo de excepciones en una API?

Con tres reglas:

1. **De forma centralizada.** Un middleware captura toda excepción no controlada
   y la traduce. Así se eliminan los `try/catch` repetidos en los controladores y
   se garantiza que **toda** respuesta de error tenga el mismo formato.
2. **Distinguiendo lo previsible de lo inesperado.** Una excepción de dominio
   (registro no encontrado, regla incumplida) es un resultado esperado del
   negocio: se traduce a su código HTTP y se registra como advertencia. Una
   excepción inesperada se registra como error con traza completa y devuelve un
   500 genérico.
3. **Sin filtrar detalles internos.** Un error 500 nunca devuelve la traza al
   cliente: revelaría rutas de archivos, versiones y estructura interna. Se
   devuelve un **identificador de correlación** que también aparece en el log, de
   modo que el usuario lo reporta y el equipo lo encuentra.

En nuestra API todas las respuestas de error siguen `ProblemDetails` (RFC 7807),
con un código de error estable y, en el caso de las validaciones, el detalle por
campo.

## 7. ¿Por qué es importante el manejo de logs y qué debe registrarse?

Los logs son el único medio de saber qué pasó en un sistema en producción cuando
no se puede reproducir el problema. Sin ellos, un incidente se investiga por
conjetura.

Usamos **Serilog** con dos destinos —consola y archivo diario con rotación,
límite de tamaño y retención de 30 días— configurados desde `appsettings.json`,
de modo que los niveles se ajustan sin recompilar.

**Qué registramos:** el arranque y la siembra de datos; una línea por petición
HTTP con método, ruta, código de respuesta y duración; cada operación de negocio
—altas, cambios, eliminaciones— con el usuario responsable; los intentos de
autenticación fallidos y su motivo; las validaciones rechazadas; y la traza
completa de cualquier error no controlado.

**Qué nunca registramos:** contraseñas, hashes, tokens ni datos personales
innecesarios. Un log es un archivo que puede leer más gente de la que uno
imagina.

Un detalle que marca la diferencia: usamos **registro estructurado**. En lugar de
concatenar cadenas, cada propiedad se registra por separado, lo que permite
buscar «todas las operaciones sobre el empleado X» sin recurrir a expresiones
regulares sobre el texto.

## 8. ¿Cómo se logra que un sistema sea escalable y mantenible?

**Escalabilidad** tiene dos dimensiones:

- *Escalabilidad funcional* — poder agregar comportamiento sin reescribir lo
  existente. Se logra con el Principio Abierto/Cerrado. En este proyecto, agregar
  un tipo de empleado no modifica ninguna clase existente.
- *Escalabilidad de carga* — poder atender más volumen. Se logra con una API
  apátrida (varias instancias detrás de un balanceador), filtrado y paginación en
  la base de datos, índices que respaldan cada filtro expuesto y consultas sin
  seguimiento de cambios.

**Mantenibilidad** se construye con decisiones acumuladas:

| Práctica | Efecto |
|---|---|
| Modularidad por capas con dependencias hacia el centro | Un cambio técnico no toca las reglas de negocio |
| Nombres explícitos, sin abreviaturas | El código se lee sin diccionario |
| Cero números mágicos | Un cambio normativo es una línea en una clase de constantes |
| Configuración externa | Cambiar de motor de base de datos no requiere recompilar |
| Pruebas automatizadas | Una regresión aparece en segundos, no en producción |
| Convenciones de nomenclatura uniformes | Cualquier desarrollador nuevo predice dónde está cada cosa |

## 9. ¿Qué es la testabilidad y cómo se garantiza?

**Testabilidad** es la facilidad con que el comportamiento de un sistema puede
verificarse de forma controlada y reproducible. No es una característica que se
agrega al final: es una consecuencia del diseño.

Lo que la hace posible en esta solución:

1. **Inversión de dependencias.** Los servicios reciben interfaces, por lo que en
   las pruebas se sustituyen por dobles. `EmployeeServiceTests` verifica las
   reglas de negocio sin base de datos.
2. **Abstracción del reloj.** `IDateTimeProvider` permite fijar la fecha en las
   pruebas. Sin él, cualquier prueba que involucre fechas dependería del reloj
   del sistema y sería frágil.
3. **Abstracción de las rutas de archivo.** `IFlatFilePathResolver` permite que
   las pruebas del archivo de texto apunten a un directorio temporal que cada
   prueba crea y elimina, sin efectos secundarios.
4. **Lógica de negocio en objetos puros.** Las fórmulas de nómina viven en
   entidades sin dependencias, por lo que se prueban instanciando un objeto y
   comparando un número.

Un ejemplo del valor concreto: el requisito no funcional «procesar 1,000
empleados en menos de 2 segundos» se verifica con una prueba automatizada, no por
inspección visual. Si una futura optimización lo rompe, la prueba lo señala.

---

## Cierre

Quedo a disposición para presentar la solución, recorrer el código y responder
cualquier pregunta adicional. Si existe un cuestionario formal de
conceptualización que no llegó con los archivos adjuntos, le agradezco
compartirlo y ajusto las respuestas a sus preguntas exactas.

Saludos cordiales,

**[Nombre completo]**
[Cargo / posición a la que aplica]
[Teléfono] · [correo]
