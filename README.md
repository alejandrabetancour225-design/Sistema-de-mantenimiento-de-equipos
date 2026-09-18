# Backend.API

API .NET 10 con autenticación JWT y base de datos PostgreSQL (Docker).

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (o cualquier PostgreSQL en local)

## Puesta en marcha

> Puedes levantar la base de datos con **Docker** (opción recomendada, pasos 1-4) o con un **PostgreSQL instalado localmente** (ver sección "Opción alternativa" más abajo).

### 1. Levantar la base de datos (Docker)

Desde la raíz del proyecto:

```bash
docker compose up -d
```

Esto crea un contenedor PostgreSQL con la base de datos `database`, usuario `postgres` y contraseña `postgres` (configurable con la variable de entorno `POSTGRES_PASSWORD`).

### 2. Configurar los secretos locales (User Secrets)

La API no guarda secretos en el repositorio. Ejecuta una vez por máquina:

```bash
dotnet user-secrets init --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Jwt:Key" "usa-una-clave-larga-y-aleatoria" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Password" "postgres" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Encryption:Key" "<clave-base64-de-32-bytes>" --project src/Backend.API/Backend.API.csproj
```

Para generar `Encryption:Key` (PowerShell):

```powershell
$b = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
[Convert]::ToBase64String($b)
```

> Si el valor de `Jwt:Key` o `Encryption:Key` ya existe en tu equipo, consérvalo (el equipo debe compartir la misma clave si se validan tokens o se comparte la misma base de datos). En producción se leen de variables de entorno (`Jwt__Key`, `Database__Password`, `Encryption__Key`).
>
> `Encryption:Key` **no debe cambiarse** después de guardar datos: se usa para descifrar los correos y teléfonos.

### 3. Crear la base de datos (migraciones)

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Backend.API --startup-project src/Backend.API
```

### 4. Ejecutar la API

```bash
dotnet run --project src/Backend.API
```

La API queda disponible en `http://localhost:5255` (dev). También es posible abrir la solución `Backend.slnx` en Visual Studio y presionar F5.

## Opción alternativa: correr sin Docker (PostgreSQL local)

Si no quieres usar Docker, instala PostgreSQL directamente en tu máquina:

1. **Instala PostgreSQL** desde https://www.postgresql.org/download/ (acepta los valores por defecto: puerto `5432`, usuario `postgres`, deja que se registre como servicio de Windows y define la contraseña que usará el servicio).

2. **Crea la base de datos** `database` con `psql`:

   ```bash
   psql -U postgres -c "CREATE DATABASE database;"
   ```

3. **Asegúrate de que la contraseña coincida** con la que la API espera (`Database:Password`, por defecto `postgres`). Si elegiste otra en la instalación, actualiza el secreto:

   ```bash
   dotnet user-secrets set "Database:Password" "tu-contraseña" --project src/Backend.API/Backend.API.csproj
   ```

4. **Configura los secretos** (si aún no los habías definido en esta máquina):

   ```bash
   dotnet user-secrets init --project src/Backend.API/Backend.API.csproj
   dotnet user-secrets set "Jwt:Key" "usa-una-clave-larga-y-aleatoria" --project src/Backend.API/Backend.API.csproj
   dotnet user-secrets set "Database:Password" "tu-contraseña" --project src/Backend.API/Backend.API.csproj
   dotnet user-secrets set "Encryption:Key" "<clave-base64-de-32-bytes>" --project src/Backend.API/Backend.API.csproj
   ```

5. **Aplica las migraciones** (crea las tablas):

   ```bash
   dotnet tool restore
   dotnet tool run dotnet-ef database update --project src/Backend.API --startup-project src/Backend.API
   ```

6. **Ejecuta la API**:

   ```bash
   dotnet run --project src/Backend.API
   ```

> No hay que cambiar nada en el código: el proyecto solo necesita un PostgreSQL accesible en `localhost:5432` con la base `database`, igual que hace Docker. Si tu PostgreSQL local usa otro puerto u host, ajústalos en `Database:Port` / `Database:Host` (appsettings o User Secrets).

## Endpoints

| Método | Ruta                       | Autorización        | Descripción                          |
|--------|----------------------------|---------------------|--------------------------------------|
| POST   | `/api/auth/register`       | Anónimo             | Registra un usuario y devuelve token |
| POST   | `/api/auth/login`          | Anónimo             | Inicia sesión y devuelve token       |
| GET    | `/api/roles`               | Administrador       | Lista los roles disponibles          |
| GET    | `/api/users`               | Administrador       | Lista los usuarios con su rol        |
| PUT    | `/api/users/{id}`          | Administrador       | Edita estado, rol, teléfono y correo |
| PUT    | `/api/users/{id}/role`     | Administrador       | Asigna solo el rol a un usuario      |
| GET    | `/api/equipment/GetEquipmentList`           | Autenticado         | Lista todos los equipos        |
| GET    | `/api/equipment/GetEquipmentById/{id}`      | Autenticado         | Consulta un equipo             |
| POST   | `/api/equipment/PostNewEquipment`           | Administrador/Técnico | Crea un equipo               |
| PUT    | `/api/equipment/PutEquipment/{id}`          | Administrador/Técnico | Edita un equipo              |
| PATCH  | `/api/equipment/PatchEquipmentStatus/{id}`  | Administrador/Técnico | Cambia el estado del equipo  |
| DELETE | `/api/equipment/DeleteEquipment/{id}`       | Administrador/Técnico | Elimina un equipo            |

**Registro**

```json
{
  "fullName": "Juan Pérez",
  "email": "juan@test.com",
  "password": "secreto123",
  "phone": "3001234567"
}
```

> `phone` es opcional.

**Login**

```json
{
  "email": "juan@test.com",
  "password": "secreto123"
}
```

Ambos devuelven `{ "token": "...", "fullName": "...", "email": "...", "role": "Cliente", "active": true }`. Usa el token en el header `Authorization: Bearer <token>` para los endpoints protegidos.

## Probar la API

Con la API corriendo en `http://localhost:5255`:

- **OpenAPI** (para importar en Postman/Insomnia): `http://localhost:5255/openapi/v1.json`
- **Archivo de peticiones**: `src/Backend.API/Backend.API.http` (se abre directo en Visual Studio o Rider). Reemplaza `{{token}}` por el token devuelto por el login y `{{equipmentId}}` por el `id` de un equipo.

Obtener un token desde la terminal:

```bash
curl -s -X POST http://localhost:5255/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"tu-correo@test.com\",\"password\":\"tu-contraseña\"}"
```

> Recuerda: el **primer usuario registrado** es el `Administrador`. Si la base está vacía, regístrate primero con ese correo.

## Roles

El sistema tiene 4 roles fijos (no se crean ni eliminan): `Administrador`, `Técnico`, `Empleado` y `Cliente`. Cada usuario tiene **un solo rol**.

- El **primer usuario registrado** recibe automáticamente el rol `Administrador`; los siguientes reciben `Cliente`.
- Solo un usuario con rol `Administrador` puede listar usuarios/roles y asignar roles.

**Asignar un rol**

```json
PUT /api/users/{id}/role
Authorization: Bearer <token-de-administrador>

{
  "role": "Técnico"
}
```

**Editar usuario** (estado, rol, teléfono y correo)

```json
PUT /api/users/{id}
Authorization: Bearer <token-de-administrador>

{
  "active": false,
  "role": "Empleado",
  "phone": "3009876543",
  "email": "nuevo@test.com"
}
```

Solo se actualizan los campos que envíes; todos son opcionales. Responde `200` con el usuario actualizado, `400` si el rol no es uno de los 4 válidos, `403` si quien llama no es Administrador, `404` si el usuario no existe y `409` si el correo ya lo usa otro usuario.

## Estado del usuario

El estado se guarda en el campo booleano `Active` (`true` = activo, `false` = inactivo). Solo un `Administrador` puede cambiarlo. Un usuario inactivo **no puede iniciar sesión** (el login responde `401`).

Además, ante cada intento de login fallido se incrementa `failedAttempts`, y se reinicia a `0` al iniciar sesión correctamente. Si `failedAttempts` supera **3**, la cuenta se **desactiva automáticamente** (`active = false`) y ya no puede iniciar sesión; un `Administrador` debe reactivarla (al reactivarla, el siguiente login correcto reinicia el contador).

## Datos sensibles (cifrado)

El `email` y el `phone` se guardan **cifrados** en la base de datos con `AES-256-GCM` (clave en `Encryption:Key`). La API los devuelve descifrados en las respuestas.

Como el correo cifrado no permite buscarlo por igualdad, se guarda además `EmailHash`, un índice ciego (`HMAC-SHA256` determinista) que se usa para el login y para la unicidad del correo. Por eso **nunca** debe cambiarse `Encryption:Key` una vez que hay datos guardados: dejarías de poder descifrar correos/teléfonos.

## Equipos

La entidad `Equipment` tiene los campos: `id`, `internalCode`, `serialNumber`, `type`, `brand`, `model`, `characteristics` (JSON libre, se guarda como `jsonb`), `acquisitionDate`, `acquisitionPrice`, `warrantyUntil`, `location`, `status`, `createdAt`, `updatedAt`.

- **Consultar** la lista o un equipo: cualquier usuario autenticado.
- **Crear, editar, cambiar estado y eliminar**: solo `Administrador` y `Técnico`.
- `internalCode` y `serialNumber` son **únicos** (si se repiten, responde `409`).

**Estados** (`EquipmentStatus`, se serializan como texto): `AVAILABLE`, `IN_USE`, `UNDER_MAINTENANCE`, `OUT_OF_SERVICE`, `DECOMMISSIONED`. Al crear, si no se envía `status`, queda en `AVAILABLE`.

**Crear equipo**

```json
POST /api/equipment/PostNewEquipment
Authorization: Bearer <token-admin-o-tecnico>

{
  "internalCode": "EQ-001",
  "serialNumber": "SN-ABC-123",
  "type": "Laptop",
  "brand": "Dell",
  "model": "Latitude 5420",
  "characteristics": { "ram": "16GB", "cpu": "i7", "storage": "512GB SSD" },
  "acquisitionDate": "2024-03-15",
  "acquisitionPrice": 1200.50,
  "warrantyUntil": "2027-03-15",
  "location": "Oficina 201"
}
```

**Editar equipo** (`PUT /api/equipment/PutEquipment/{id}`): solo se actualizan los campos que envíes.

**Cambiar estado**

```json
PATCH /api/equipment/PatchEquipmentStatus/{id}
Authorization: Bearer <token-admin-o-tecnico>

{
  "status": "IN_USE"
}
```

**Eliminar**: `DELETE /api/equipment/DeleteEquipment/{id}` responde `204` (o `404` si no existe).

## CORS (conexión desde el front)

En desarrollo se aceptan peticiones de cualquier origen en `localhost` (React, Angular, etc.), no hay que configurar nada.

Para producción, define los orígenes permitidos en `appsettings.json` (o con la variable de entorno `Cors__AllowedOrigins__0`):

```json
"Cors": {
  "AllowedOrigins": ["https://tu-front.example.com"]
}
```

## Base de datos

- Base: `database`
- Usuario: `postgres`
- Contraseña: `postgres` (por defecto)
- Puerto: `5432`

Tabla `Users`: `Id`, `Email` (cifrado), `EmailHash` (índice ciego), `PasswordHash`, `FullName`, `Phone` (cifrado), `Active` (booleano), `FailedAttempts`, `CreatedAt`, `UpdatedAt`, `RoleId`.
Tabla `Roles`: `Id`, `Name` (4 roles fijos).
Tabla `Equipments`: `Id`, `InternalCode` (único), `SerialNumber` (único), `Type`, `Brand`, `Model`, `Characteristics` (jsonb), `AcquisitionDate`, `AcquisitionPrice`, `WarrantyUntil`, `Location`, `Status`, `CreatedAt`, `UpdatedAt`.

Para conectarse desde un cliente (DBeaver, pgAdmin): host `localhost`, puerto `5432`, base `database`, usuario `postgres`, contraseña `postgres`.