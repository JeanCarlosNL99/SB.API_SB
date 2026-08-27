# Guía de instalación y ejecución local

Instrucciones paso a paso para clonar este repositorio desde GitHub, instalarlo
y ejecutarlo en una máquina nueva. Si ya tiene el proyecto abierto y solo
necesita el resumen de comandos, vea la sección **2. Ejecución rápida** del
[`README.md`](README.md); esta guía es la versión detallada, pensada para quien
parte de cero.

---

## 1. Requisitos previos

Instale, en este orden, lo que no tenga ya:

| Herramienta | Versión mínima | Dónde descargarla |
|---|---|---|
| **Git** | 2.x | https://git-scm.com/downloads |
| **.NET SDK** | 8.0 | https://dotnet.microsoft.com/download/dotnet/8.0 |
| **Node.js** | 18.x (incluye npm) | https://nodejs.org/ (elija la versión LTS) |

No necesita instalar SQL Server, Oracle ni ningún motor de base de datos aparte:
la aplicación usa **SQLite** por defecto y crea su propio archivo de base de
datos en el primer arranque. SQL Server es opcional (sección 7).

### 1.1 Verificar la instalación

Abra una terminal (PowerShell, Git Bash o la terminal de su editor) y ejecute:

```bash
git --version
dotnet --version
node --version
npm --version
```

Debe ver una versión de Git, una versión `8.x.x` (o superior) de .NET y una
versión `18.x` o más reciente de Node. Si algún comando no se reconoce, cierre y
vuelva a abrir la terminal después de instalar la herramienta correspondiente
(en Windows, a veces hace falta reiniciar la sesión para que el `PATH` se
actualice).

> **Nota sobre el runtime de .NET.** El proyecto está configurado con
> `<RollForward>LatestMajor</RollForward>`, por lo que también se ejecuta si su
> máquina tiene instalado un runtime más nuevo que 8.0 (por ejemplo, .NET 9 o
> .NET 10) en lugar del propio 8.0. Si `dotnet --version` muestra un número
> mayor, no hace falta instalar la 8.0 aparte.

---

## 2. Clonar el repositorio

```bash
git clone <URL-DEL-REPOSITORIO>
cd <NOMBRE-DE-LA-CARPETA>
```

Reemplace `<URL-DEL-REPOSITORIO>` por la URL que copió de GitHub (botón verde
**Code** → **HTTPS**, algo como
`https://github.com/usuario/SB.API_SB.git`), y `<NOMBRE-DE-LA-CARPETA>` por el
nombre que Git le dio a la carpeta clonada (normalmente el nombre del
repositorio).

Verifique que está en la carpeta correcta:

```bash
ls
```

Debe ver, entre otros, los archivos `SB.API_SB.sln`, `README.md` y las carpetas
`src/`, `tests/`, `frontend/` y `docs/`.

---

## 3. Poner en marcha el backend (la API)

### 3.1 Restaurar dependencias y compilar

Desde la raíz del proyecto:

```bash
dotnet restore
dotnet build
```

El primer comando descarga los paquetes NuGet que usa la solución; el segundo
compila los cinco proyectos. Si termina con `Build succeeded`, todo está listo
para ejecutar.

### 3.2 Ejecutar la API

```bash
dotnet run --project src/SB.API_SB.Presentation
```

La primera vez que se ejecuta, la aplicación hace automáticamente tres cosas
antes de aceptar peticiones:

1. **Genera el listado de entidades gubernamentales**
   (`src/SB.API_SB.Presentation/Database/GovernmentEntities.txt`) a partir del
   archivo semilla versionado, con las 181 entidades oficiales.
2. **Crea la base de datos relacional** (un archivo SQLite en
   `src/SB.API_SB.Presentation/Database/SB_API_SB.db`) y siembra los roles, el
   usuario administrador, los departamentos y **11 empleados de demostración**
   repartidos entre **4 entidades gubernamentales** reales del listado.
3. **Genera un historial de nómina** de 32 ejecuciones (8 semanas anteriores por
   cada una de esas 4 entidades), para que el módulo de pagos tenga datos desde
   el primer momento.

Cuando la consola muestre una línea similar a:

```
Now listening on: http://localhost:5080
```

la API está lista. Dos formas rápidas de confirmarlo:

- Abra **http://localhost:5080/swagger** en el navegador: debe aparecer la
  documentación interactiva de todos los endpoints.
- O ejecute, en otra terminal:

  ```bash
  curl http://localhost:5080/api/estado
  ```

  Debe responder `200 OK`.

**Deje esta terminal abierta** — el proceso queda corriendo en primer plano.
Para detenerlo más adelante, vuelva a esa terminal y presione `Ctrl+C`.

---

## 4. Poner en marcha el frontend (el portal web)

Abra una **segunda terminal** (sin cerrar la de la API) y, desde la raíz del
proyecto:

### 4.1 Instalar dependencias

```bash
cd frontend
npm install
```

Esto descarga las dependencias de Node declaradas en `package.json`. Solo hace
falta ejecutarlo una vez (o cada vez que cambien las dependencias).

### 4.2 Ejecutar el portal

```bash
npm run dev
```

Cuando la terminal muestre algo como:

```
➜  Local:   http://localhost:5173/
```

abra esa dirección en el navegador. El servidor de desarrollo de Vite redirige
automáticamente las peticiones a `/api` hacia `http://localhost:5080`, así que
no hace falta configurar ninguna URL adicional siempre que la API (paso 3) siga
corriendo.

**Deje esta segunda terminal abierta** también. Para detenerla, `Ctrl+C` en esa
misma terminal.

---

## 5. Iniciar sesión

En **http://localhost:5173** use las credenciales sembradas automáticamente:

| Usuario | Contraseña |
|---|---|
| `administrador` | `Sb2024Admin` |

Esa contraseña se define en `src/SB.API_SB.Presentation/appsettings.json`, en la
sección `Seed`. Es un valor de desarrollo pensado para evaluar la prueba
técnica: **no la use tal cual en un entorno real**; para producción, muévala a
variables de entorno o a `dotnet user-secrets` y cambie también la
`Jwt:SigningKey` de la misma sección.

---

## 6. Verificar que todo funciona (opcional pero recomendado)

Con ambos servidores corriendo, en una tercera terminal puede ejecutar las
pruebas automatizadas para confirmar que el clon quedó íntegro:

```bash
dotnet test
```

Debe reportar `107` pruebas aprobadas y ninguna fallida.

```bash
cd frontend
npm run typecheck
npm run lint
```

Ambos deben terminar sin errores.

---

## 7. Cambiar a SQL Server (opcional)

Por defecto la aplicación usa SQLite y no requiere ninguna instalación
adicional. Si prefiere usar SQL Server:

1. Edite `src/SB.API_SB.Presentation/appsettings.json` (o, mejor, cree un
   `appsettings.Local.json` junto a él — ese archivo está excluido del control
   de versiones) y cambie:

   ```json
   "Database": {
     "Provider": "SqlServer"
   }
   ```

2. Ajuste la cadena `ConnectionStrings:SqlServerConnection` con los datos de su
   instancia (servidor, usuario y contraseña).
3. Vuelva a ejecutar `dotnet run --project src/SB.API_SB.Presentation`. El
   esquema se crea automáticamente igual que con SQLite; no hace falta ejecutar
   migraciones.

No es necesario cambiar nada más: el `DbContext` y los mapeos de Entity
Framework Core son los mismos para ambos proveedores.

---

## 8. Solución de problemas frecuentes

| Síntoma | Causa probable | Solución |
|---|---|---|
| `dotnet: command not found` (o similar) | El SDK de .NET no está instalado o no está en el `PATH` | Instale el SDK 8.0 y abra una terminal nueva |
| El puerto `5080` o `5173` ya está en uso | Otra instancia de la API o del frontend sigue corriendo | Cierre el proceso anterior (`Ctrl+C` en su terminal), o busque y detenga el proceso que ocupa el puerto |
| El frontend carga pero los datos no aparecen / errores de red en la consola del navegador | La API no está corriendo, o se detuvo | Verifique que la terminal del paso 3 siga activa y que `http://localhost:5080/api/estado` responda |
| `npm install` falla con errores de permisos | Configuración de npm en el sistema | Vuelva a intentar en una terminal con permisos de usuario normales (evite `sudo npm install`) |
| Tras `git pull` el build falla o el frontend no arranca | Cambiaron las dependencias | Repita `dotnet restore` y, en `frontend/`, `npm install` |
| Quiere empezar de nuevo con datos limpios | La base de datos y el listado generado ya existen en disco | Detenga la API y elimine `src/SB.API_SB.Presentation/Database/SB_API_SB.db*` y `src/SB.API_SB.Presentation/Database/GovernmentEntities.txt`; se regeneran solos en el siguiente arranque |

---

## 9. Resumen de comandos

Para quien ya siguió esta guía una vez y solo necesita la referencia rápida en
el futuro:

```bash
# Terminal 1 — backend
dotnet run --project src/SB.API_SB.Presentation

# Terminal 2 — frontend
cd frontend && npm run dev
```

API: http://localhost:5080 (Swagger en `/swagger`) · Portal:
http://localhost:5173

---

Para el detalle de la arquitectura, los endpoints disponibles y las decisiones
de diseño, vea [`README.md`](README.md) y
[`docs/REPORTE-TECNICO.md`](docs/REPORTE-TECNICO.md).
