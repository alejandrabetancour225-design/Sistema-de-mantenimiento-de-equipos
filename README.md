# Backend.API

API .NET 10 con autenticación JWT y base de datos PostgreSQL.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (o cualquier PostgreSQL en local)

## Puesta en marcha

La API usa la base PostgreSQL **compartida en Neon** (host, base y usuario en `appsettings.json`, sección `Database`, con `SslMode=Require`). Todo el equipo trabaja sobre la misma base.

### 1. Configurar los secretos (User Secrets)

La API no guarda secretos en el repositorio. **Nunca** los pongas en archivos dentro de la carpeta del proyecto (`setup.ps1`, `.md`, etc.): se terminan subiendo a GitHub. Compártelos con el equipo por un canal privado.

```bash
dotnet user-secrets init --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Password" "<password-de-neon>" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Jwt:Key" "<clave-aleatoria-de-al-menos-32-caracteres>" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Encryption:Key" "<clave-base64-de-32-bytes>" --project src/Backend.API/Backend.API.csproj

# Administrador inicial (solo se usa si no existe ningún Administrador activo)
dotnet user-secrets set "Bootstrap:AdminEmail" "admin@tu-dominio.com" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Bootstrap:AdminPassword" "<contraseña-fuerte>" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Bootstrap:AdminFullName" "Administrador" --project src/Backend.API/Backend.API.csproj
```

`Jwt:Key` y `Encryption:Key` deben ser **iguales en todo el equipo** (con otra `Encryption:Key` no se pueden descifrar los correos y teléfonos guardados en Neon).

Para generar una clave (PowerShell):

```powershell
$b = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
[Convert]::ToBase64String($b)
```

> En producción se leen de variables de entorno (`Jwt__Key`, `Encryption__Key`, `Database__Password`, `Bootstrap__AdminEmail`, ...).

### 2. Aplicar las migraciones (una sola vez por cambio de esquema)

Las migraciones **no** se aplican solas al arrancar (`Database:MigrateOnStartup=false`): con una base compartida, un solo integrante las aplica cuando hay migraciones nuevas.

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Backend.API --startup-project src/Backend.API
```

### 3. Ejecutar la API

```bash
dotnet run --project src/Backend.API
```

Si no hay ningún Administrador activo, la API lo crea con los datos de `Bootstrap:*`. Después puedes borrar `Bootstrap:AdminPassword` de los user-secrets.

La API queda disponible en `http://localhost:5255`.

### Usuario de la base con permisos mínimos (recomendado)

Hoy la API se conecta con `neondb_owner`, que puede modificar o borrar tablas. Para que la API solo lea y escriba datos, crea un usuario con [`database/app_user_role.sql`](database/app_user_role.sql) (en la consola SQL de Neon) y úsalo en tus user-secrets:

```bash
dotnet user-secrets set "Database:User" "app_user" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Password" "<password-de-app_user>" --project src/Backend.API/Backend.API.csproj
```

Las migraciones se siguen aplicando con `neondb_owner`.

### Base local con Docker (opcional)

Crea un archivo `.env` junto a `docker-compose.yml` (ignorado por git) con `POSTGRES_PASSWORD=...`, ejecuta `docker compose up -d` (el puerto solo queda en `127.0.0.1`) y sobrescribe en tus user-secrets `Database:Host=localhost`, `Database:Name=database`, `Database:User=postgres`, `Database:Password=<la del .env>` y `Database:SslMode=Disable`.

## Seguridad

### Autenticación y sesión

- El login y el registro devuelven el token **y** lo dejan en una cookie `httpOnly` (`access_token`, `SameSite=Strict`, `Secure` fuera de desarrollo). La API acepta el token por la cabecera `Authorization: Bearer` o por la cookie.
- Mientras el frontend no use la cookie, el token se sigue devolviendo en el cuerpo (`Auth:ReturnTokenInBody=true`). Cuando el frontend use `withCredentials: true`, ponlo en `false`.
- Las peticiones que modifican datos y se autentican **con la cookie** deben enviar la cabecera `X-CSRF: 1` (protección CSRF).
- En cada petición se valida contra la base que el usuario siga activo, que su rol no haya cambiado y que su **sello de seguridad** (`SecurityStamp`) sea el mismo. Cambiar el rol, desactivar, cambiar el correo, restablecer la contraseña o cerrar sesión invalida los tokens anteriores de inmediato.
- `GET /api/auth/me` devuelve el usuario de la sesión; `POST /api/auth/logout` cierra todas sus sesiones.

### Contraseñas y bloqueo

- Mínimo 10 caracteres (máximo 128), con al menos una letra y un número.
- Hash PBKDF2-SHA256 con 600.000 iteraciones. Los hashes antiguos se regeneran solos al iniciar sesión.
- Tras **5 intentos fallidos** la cuenta queda **bloqueada 15 minutos** (todos los roles, incluido el Administrador). Ya no se desactiva la cuenta: nadie puede dejar fuera a otro de forma permanente. Un Administrador puede levantar el bloqueo antes enviando `"active": true`.
- El login responde igual (y tarda lo mismo) si el correo no existe, si la contraseña es incorrecta o si la cuenta está inactiva o bloqueada.
- Cualquier usuario cambia su propia contraseña con `POST /api/auth/change-password` (pide la actual). Se cierran sus demás sesiones y la actual sigue con un token nuevo. Los intentos con la contraseña actual incorrecta cuentan para el bloqueo temporal.
- "Olvidé mi contraseña": el Administrador la restablece con `PUT /api/users/{id}/password`.

### Límites

- General: 300 peticiones por minuto por usuario (o por IP sin sesión).
- Login: 10 por minuto por IP. Registro: 5 cada 15 minutos por IP.
- Cuerpo de la petición: máximo 1 MB. Textos con largo máximo y JSON libre (`characteristics`, `specifications`) de máximo 8.000 caracteres.
- Todos configurables en `appsettings.json` (`RateLimiting`, `Limits`).

### Errores y auditoría

- Los errores no controlados responden `{ "message", "traceId" }` sin detalles internos; el detalle queda en el log con el mismo `traceId`.
- Toda alta, cambio o borrado queda en la tabla `AuditLogs` (usuario, IP, entidad, campos modificados), igual que los inicios de sesión, fallos y bloqueos. Consulta: `GET /api/audit` (solo Administrador; filtros `userId`, `entityType`, `entityId`, `action`, `take`).

### Cabeceras

`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy`, `Cache-Control: no-store`, HSTS fuera de desarrollo y sin cabecera `Server`.

## Roles y permisos

Hay 4 roles fijos: `Administrador`, `Técnico`, `Empleado` y `Cliente`. Cada usuario tiene **un solo rol**.

- El **registro público siempre crea `Cliente`**. El primer Administrador se crea desde la configuración (`Bootstrap:*`).
- No se puede desactivar ni quitar el rol al **último Administrador activo**, y un Administrador no puede desactivarse ni quitarse el rol a sí mismo.

| Módulo | Administrador | Técnico | Empleado | Cliente |
| --- | --- | --- | --- | --- |
| Usuarios y roles | Todo | — | — | — |
| Auditoría | Consulta | — | — | — |
| Equipos | Todo | Todo | Consulta | — |
| Componentes | Todo | Todo | Consulta | — |
| Asignaciones | Todo | Todo | Solo las suyas | — |
| Incidentes | Todo | Consulta | Reportar; editar/borrar solo los suyos abiertos | — |
| Mantenimiento | Todo | Todo | Consulta | Consulta del historial |
| Repuestos | Todo | Todo | — | — |

## Endpoints

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Anónimo | Registra un **Cliente** y abre sesión |
| POST | `/api/auth/login` | Anónimo | Inicia sesión |
| GET | `/api/auth/me` | Autenticado | Usuario de la sesión |
| POST | `/api/auth/change-password` | Autenticado | Cambia la contraseña propia |
| POST | `/api/auth/logout` | Autenticado | Cierra todas las sesiones del usuario |
| GET | `/api/roles` | Administrador | Lista los roles |
| GET | `/api/users` | Administrador | Lista los usuarios |
| PUT | `/api/users/{id}` | Administrador | Edita estado, rol, teléfono y correo |
| PUT | `/api/users/{id}/role` | Administrador | Asigna el rol |
| PUT | `/api/users/{id}/password` | Administrador | Restablece la contraseña |
| GET | `/api/audit` | Administrador | Eventos de auditoría |
| GET | `/api/equipment/GetEquipmentList` | Adm./Téc./Emp. | Lista equipos |
| GET | `/api/equipment/GetEquipmentById/{id}` | Adm./Téc./Emp. | Consulta un equipo |
| POST | `/api/equipment/PostNewEquipment` | Adm./Téc. | Crea un equipo |
| PUT | `/api/equipment/PutEquipment/{id}` | Adm./Téc. | Edita un equipo |
| PATCH | `/api/equipment/PatchEquipmentStatus/{id}` | Adm./Téc. | Cambia el estado |
| DELETE | `/api/equipment/DeleteEquipment/{id}` | Adm./Téc. | Da de baja |
| POST | `/api/assignment/AssignEquipment` | Adm./Téc. | Asigna un equipo |
| PATCH | `/api/assignment/ReleaseEquipment/{id}` | Adm./Téc. | Libera un equipo |
| GET | `/api/assignment/GetAssignments` | Adm./Téc./Emp. | Lista (Empleado: solo las suyas) |
| GET | `/api/equipmentcomponent/...` | Adm./Téc./Emp. | Consulta componentes |
| POST/PUT/DELETE | `/api/equipmentcomponent/...` | Adm./Téc. | Gestiona componentes |
| GET | `/api/incident/...` | Adm./Téc./Emp. | Consulta incidentes |
| POST/PUT/DELETE | `/api/incident/...` | Adm./Emp. | Gestiona incidentes |
| GET | `/api/maintenance/...` | Todos los roles | Consulta mantenimientos e historial |
| POST/PUT/DELETE | `/api/maintenance/...` | Adm./Téc. | Gestiona mantenimientos |
| GET/POST/PUT/PATCH | `/api/sparepart/...` | Adm./Téc. | Repuestos |

### **Registro**

```json
{
  "fullName": "Juan Pérez",
  "email": "juan@test.com",
  "password": "secreto123",
  "phone": "3001234567"
}
```

Login y registro devuelven `{ "token", "userId", "fullName", "email", "role", "active", "expiresAt" }` y dejan la cookie de sesión.

Los errores de validación responden `400` con `{ "message": "...", "errors": { "campo": ["..."] } }`.

## Reglas de negocio

### Equipos

- `internalCode` y `serialNumber` son únicos (`409`).
- `UNDER_MAINTENANCE` solo lo pone el flujo de mantenimiento; con un mantenimiento abierto, el estado del equipo cambia al cerrarlo o cancelarlo.
- La garantía no puede terminar antes de la fecha de compra; el precio no puede ser negativo.
- En la baja de equipo, `DELETE /api/equipment/DeleteEquipment/{id}` no borra físicamente, sino que cambia el estado a `DECOMMISSIONED` y conserva todo el historial. Responde `409` si el equipo ya está dado de baja, si tiene una asignación `ACTIVE` o si tiene un mantenimiento `OPEN`/`IN_PROGRESS`.
- Un equipo `DECOMMISSIONED` no puede volver a `AVAILABLE` ni asignarse.

### Asignaciones

- Un equipo solo puede tener una asignación `ACTIVE` (garantizado también por un índice único, incluso con peticiones simultáneas).
- No se asignan equipos en mantenimiento, fuera de servicio o dados de baja, ni a usuarios inactivos.

### Incidentes

- Un equipo solo puede tener un incidente `OPEN` o `IN_PROGRESS` (índice único).
- No se reportan incidentes de equipos dados de baja ni con fecha futura.
- `IN_PROGRESS` solo lo pone el mantenimiento; con un mantenimiento asociado, el estado lo maneja ese mantenimiento.
- El Empleado solo edita o borra **sus** incidentes mientras están `OPEN` y sin mantenimiento, y solo puede pasarlos a `CLOSED`.
- Un incidente con mantenimiento asociado no se borra.

### Mantenimiento

- Un equipo solo puede tener un mantenimiento `OPEN` o `IN_PROGRESS` (índice único).
- La incidencia vinculada debe ser del mismo equipo y estar `OPEN`.
- Costos y cantidades no pueden ser negativos; la fecha de inicio no puede ser futura y el próximo mantenimiento no puede ser anterior al inicio.
- **Completar solo con `POST /api/maintenance/CloseMaintenance/{id}`** (exige el trabajo realizado). `PutMaintenance` no acepta `COMPLETED`.
- Al cerrar, el equipo no puede quedar `UNDER_MAINTENANCE` y la incidencia queda `RESOLVED` o `CLOSED`.
- Cancelar libera el equipo y devuelve la incidencia a `OPEN` (se desvincula y queda anotado en las observaciones).
- Un mantenimiento completado no se borra (es historial y costo del equipo).

### Enums

Se aceptan solo por nombre (`"OPEN"`); un número (`99`) o un nombre inexistente responde `400`.

## Datos sensibles (cifrado)

`email` y `phone` se guardan cifrados con AES-256-GCM. Desde esta versión el cifrado usa el formato `v2`, que **ata cada valor a su usuario y campo** (datos asociados): un valor copiado a otro usuario o columna ya no se puede descifrar. Los valores del formato anterior se siguen leyendo y se migran al iniciar sesión o con la rotación.

`EmailHash` es un índice ciego (HMAC-SHA256) para buscar por correo sin descifrar.

**Rotar `Encryption:Key`**: pon la clave vieja en `Encryption:PreviousKeys`, la nueva en `Encryption:Key` y arranca una vez con `Encryption__RotateOnStartup=true`. La API vuelve a cifrar todo con la clave nueva, lo verifica y se detiene. Luego quita la clave vieja de `PreviousKeys`.

> `fullName` no se cifra porque se usa para ordenar y se muestra en asignaciones, incidentes y mantenimientos.

## Base de datos

Tablas: `Users` (con `Email`/`Phone` cifrados, `EmailHash`, `LockoutEnd`, `SecurityStamp`), `Roles`, `Equipments`, `Assignments`, `EquipmentComponents`, `Incidents`, `Maintenances`, `SpareParts`, `MaintenanceSpareParts` y `AuditLogs`.

## Probar la API

- **OpenAPI** (solo en desarrollo): `http://localhost:5255/openapi/v1.json`
- **Archivo de peticiones**: `src/Backend.API/Backend.API.http`.

## CORS

En desarrollo se acepta cualquier origen `localhost` (con credenciales). En producción define los orígenes en `Cors:AllowedOrigins` (o `Cors__AllowedOrigins__0`).
