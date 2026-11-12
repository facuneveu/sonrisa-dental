# Despliegue y entrega

La guía del TFI pide, para la entrega, **tres cosas**:

1. El enlace al repositorio de GitHub (con `README.md`).
2. Una **URL pública** donde se pueda probar el sistema funcionando.
3. Credenciales de prueba (un administrador y un cliente/paciente ya creados).

El proyecto ya está preparado para publicarse. Lo que falta hacer es solo lo que requiere **tus cuentas** (GitHub y el hosting): ninguna de las dos cosas se puede hacer desde acá.

> ⚠️ **Antes de nada:** el sistema trabaja con datos ficticios. No cargues datos reales de personas, y si dejás el sitio público después de la evaluación, cambiá la contraseña del administrador desde *Mi perfil* (la de los datos de ejemplo está en este repositorio).

---

## 1. Subir el código a GitHub

El repositorio **ya está inicializado** (rama `main`, con el historial por hitos); solo falta conectarlo a GitHub. Creá antes un repositorio **vacío** en tu cuenta (sin README ni `.gitignore`, que ya existen acá) y, desde la carpeta raíz del proyecto (la que contiene `ClinicaDental.sln`):

```bash
git status
git remote add origin https://github.com/TU-USUARIO/sonrisa-dental.git
git push -u origin main
```

`git status` tiene que decir que no hay nada para confirmar. El `.gitignore` ya excluye `bin/`, `obj/`, secretos y archivos de usuario, y no hay ninguna clave en el historial, pero **revisalo antes del primer `push`** (`git log -p` o `git grep AIza`): la guía remarca que una API key subida a un repositorio público se invalida sola.

> La guía evalúa el historial de commits. De acá en adelante, hacé commits chicos y con mensajes claros (uno por cambio) en lugar de uno grande.

## 2. Variables de entorno

La aplicación no lleva secretos en el código. Todo lo que cambia entre tu máquina y el hosting se configura con variables de entorno (en .NET, el `:` de la configuración se escribe `__`).

| Variable | Para qué sirve | Valor |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a SQL Server. **Obligatoria.** | La que te da el hosting |
| `ASPNETCORE_ENVIRONMENT` | Modo de ejecución | `Production` |
| `SeedDatosEjemplo` | Crea los datos y las cuentas de prueba al iniciar | `true` (necesario para que existan las credenciales de la entrega) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Respeta el HTTPS que pone el proxy del hosting | `true` (ya viene en el `Dockerfile`) |
| `PORT` | Puerto en el que escuchar | Lo define el hosting (Render lo hace solo) |
| `DataProtection__KeysPath` | Carpeta persistente para las claves de sesión. **Obligatoria en un hosting compartido de Windows (IIS)**: sin ella el login puede dar error 500 o cerrar la sesión en cada reinicio. Una ruta relativa se toma desde la carpeta de la aplicación | En MonsterASP.NET: `..\private\keys`. En otros hostings, una carpeta que persista |
| `Gemini__ApiKey` | Clave de la API de IA del asistente virtual. Sin ella el sistema funciona y el chat avisa que no está configurado | Tu clave de [Google AI Studio](https://aistudio.google.com/apikey). **Nunca en el repositorio** |
| `Gemini__Modelo` | *(Opcional)* fuerza un modelo de Gemini; si no, se prueba la lista de `appsettings.json` | Por ejemplo `gemini-3.5-flash-lite` |
| `Asistente__ConsultasPorMinuto` | *(Opcional)* tope de consultas al chat por persona | `8` por defecto |

Al arrancar, la aplicación **crea las tablas por sí sola** (aplica las migraciones) y carga los datos de ejemplo. El usuario de la base necesita permiso para crear tablas.

## 3. Opciones de hosting

La app necesita **SQL Server**. Eso condiciona la elección: no todos los hostings lo ofrecen.

### Opción recomendada — MonsterASP.NET (gratis, con SQL Server)

Plan gratuito y sin tarjeta: 1 sitio (5 GB y 256 MB de RAM), 1 base de datos SQL Server de 1 GB, .NET 8 a 11 y una dirección `TU-SITIO.runasp.net`. Se publica desde Visual Studio con Web Deploy, o por FTP o ZIP. Es la que mejor encaja con este proyecto: SQL Server incluido, variables de entorno desde el panel y .NET 9.

Lo que hay que saber del plan gratuito (según su [comparación de planes](https://help.monsterasp.net/getting-started/first-steps/free-vs-premium)):

- **Se duerme por inactividad** (no tiene *AlwaysRunning*): la primera visita después de un rato tarda. Abrí el sitio unos minutos antes de la defensa.
- **HTTPS lo activa el soporte, a pedido y después de subir el sitio.** Pedilo con un ticket desde el panel apenas publiques, con tiempo.
- No envía e-mails (el sistema no los usa), no da garantías y tiene 256 MB de RAM fijos. Para una demo alcanza.

Pasos:

1. Creá la cuenta en [monsterasp.net](https://www.monsterasp.net/ASP.NET-Freehosting/) (plan *Free*). En el panel creá un **sitio** (elegí .NET 9) y una **base de datos MSSQL**.
2. En *Databases* → tu base → *Users* → *Connection string*, copiá la de **Local access** (la *Remote* es para conectarte desde tu PC). Ver [Find your connection string](https://help.monsterasp.net/databases/mssql/connection-string).
3. En tu sitio → *Scripting* → [*Environment variables*](https://help.monsterasp.net/websites/configuration/environment-variables) cargá estas variables y tocá *Restart application*:

   | Variable | Valor |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | la cadena **Local access** del paso 2 |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `SeedDatosEjemplo` | `true` |
   | `DataProtection__KeysPath` | `..\private\keys` (la carpeta privada del sitio; [por qué](https://help.monsterasp.net/websites/configuration/data-protection-keys)) |
   | `Gemini__ApiKey` | tu clave de Google AI Studio |

4. En *Deploy* → *Visual Studio access* activá **WebDeploy** y tocá *Download publish profile* (un archivo `.publishSettings`).
5. En Visual Studio: clic derecho en el proyecto `ClinicaDental` → **Publicar** → *Importar perfil* → elegí ese archivo → **Publicar**. Detalle en [Deploy from Visual Studio](https://help.monsterasp.net/websites/deploy/visual-studio). Si tu red bloquea el puerto 8172, subí la publicación por FTP.
6. Abrí `https://TU-SITIO.runasp.net/healthz` (tiene que decir `ok`) y después el sitio. El primer arranque tarda: crea las tablas y los datos de ejemplo.
7. Abrí un ticket pidiendo el HTTPS y, mientras tanto, recorré la lista de la sección 4.

Si el sitio muestra *HTTP Error 500*, mirá los registros del sitio en el panel: casi siempre es una variable mal copiada (por ejemplo, la cadena de conexión).

### Opción A — SmarterASP.NET (hosting .NET con SQL Server incluido)

Es la más directa si el plan incluye SQL Server.

1. En el panel, creá el sitio y una base de datos SQL Server. Copiá la cadena de conexión que te muestran.
2. Confirmá que el plan ofrezca **.NET 9**. Si solo llega a una versión anterior, esta opción no sirve y conviene la C o la B.
3. Publicá en una carpeta:
   ```bash
   dotnet publish ClinicaDental/ClinicaDental.csproj -c Release -o publicacion
   ```
4. Subí **el contenido** de `publicacion/` por FTP al directorio del sitio (la publicación ya incluye el `web.config` que necesita IIS).
5. Configurá la cadena de conexión. Si el panel no permite variables de entorno, creá en el servidor un archivo `appsettings.Production.json` con solo esto (**no lo subas a GitHub**):
   ```json
   { "ConnectionStrings": { "DefaultConnection": "LA-CADENA-DEL-PANEL" } }
   ```
6. Abrí la URL. El primer arranque tarda un poco: está creando la base.

### Opción B — Azure (App Service + Azure SQL)

1. Creá una **Azure SQL Database** y un **App Service** con .NET 9.
2. En Azure SQL, permití el acceso desde los servicios de Azure (firewall).
3. En el App Service → *Configuración* → *Variables de entorno* y *Cadenas de conexión*: cargá `ConnectionStrings__DefaultConnection`, `ASPNETCORE_ENVIRONMENT=Production` y `SeedDatosEjemplo=true`.
4. Publicá desde Visual Studio (*clic derecho en el proyecto → Publicar → Azure*) o con GitHub Actions.
5. Verificá la URL `https://TU-APP.azurewebsites.net/healthz`.

El App Service conserva su disco entre reinicios, así que las sesiones se mantienen.

### Opción C — Render con Docker + SQL Server externo

Render ejecuta el `Dockerfile` del repositorio, pero **no ofrece SQL Server administrado**: necesitás una base accesible desde internet (por ejemplo una Azure SQL o la base de un hosting .NET) y pegar su cadena de conexión.

1. En Render: *New → Web Service* → conectá el repositorio de GitHub.
2. *Runtime:* **Docker** (usa el `Dockerfile` de la raíz).
3. En *Environment*, cargá las variables de la tabla de arriba.
4. Creá el servicio y esperá el primer despliegue. El chequeo de salud es `/healthz`.

Cosas a tener en cuenta en Render:

- Los planes gratuitos **suelen dormirse** tras un rato sin visitas (verificá las condiciones vigentes del plan que elijas): la primera carga después tarda. Abrí el sitio unos minutos antes de la defensa.
- El disco es **temporal**. Los archivos de los pacientes no se pierden (están en la base), pero las sesiones abiertas sí se cortan al reiniciar. Si el plan permite un disco persistente, montalo y apuntá `DataProtection__KeysPath` a esa carpeta.
- ⚠️ **El `Dockerfile` no se pudo probar en la máquina donde se desarrolló (no tenía Docker instalado).** Lo que sí se verificó es la publicación en Release, la ejecución en modo Producción con puerto por `PORT` y detrás de un proxy HTTPS. Si tenés Docker, probalo antes: `docker build -t sonrisa-dental .`

## 4. Qué probar después de publicar

Una vez online, recorré esto con los tres usuarios de prueba (la pantalla de login muestra los botones para completar el formulario):

- [ ] `/healthz` responde `ok`.
- [ ] **Administrador:** crear un tratamiento, ver la auditoría.
- [ ] **Paciente:** sacar un turno, leer un mensaje, abrir su historia clínica.
- [ ] **Odontólogo:** marcar un turno como atendido y cargar la historia clínica, tocar una pieza del odontograma, **subir una radiografía**, enviar un aviso a un paciente.
- [ ] **Asistente (con `Gemini__ApiKey` cargada):** abrir el botón *Consultas*, tocar *¿Qué horarios de atención tienen?* y comprobar que responde con los horarios reales. Si dice que no está disponible, mirar el log del servicio: queda el código HTTP y el motivo que devolvió Google.

Si algo falla, los problemas más comunes son:

| Síntoma | Causa probable |
|---|---|
| Error 500 al abrir el sitio | Cadena de conexión mal copiada, o la base no acepta conexiones desde el hosting (firewall) |
| El sitio abre pero no hay usuarios | `SeedDatosEjemplo` no está en `true` |
| Se corta la sesión cada tanto | Disco temporal: ver `DataProtection__KeysPath` |
| No se pueden subir archivos grandes | Límite de 5 MB por archivo (es intencional) |

## 5. Cerrar la entrega

Cuando tengas la URL pública, completá en el `README.md` la línea **URL pública** y verificá que las credenciales de prueba que figuran ahí funcionen en el sitio publicado. Eso es lo que revisa quien corrige.
