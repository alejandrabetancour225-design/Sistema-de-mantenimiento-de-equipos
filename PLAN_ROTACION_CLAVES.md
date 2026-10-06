# Rotación de claves y limpieza de historial

Repo: `C:\Users\xXSIL\Source\APP_Mantenimiento_Equipos`
BD: Neon `neondb` (`ep-proud-dust-b4p707z7-pooler.c-6.us-east-2.aws.neon.tech`)
Remote: `https://github.com/alejandrabetancour225-design/Sistema-de-mantenimiento-de-equipos`

---

## 0. Diagnóstico (qué está filtrado y por qué importa)

`src/Backend.API/setup.ps1` estuvo versionado desde el commit `0c4450a` y sigue en `HEAD`.
Contiene **tres secretos vivos**, y los tres son los que tu equipo usa hoy:

| Secreto | Dónde se usa | Impacto si se filtra |
|---|---|---|
| `Database:Password` (`npg_…`) | `Program.cs:20` → connection string a Neon | Acceso total a la BD |
| `Encryption:Key` | `EncryptionService` → cifra `Users.Email` / `Users.Phone` | Descifrado de todos los correos y teléfonos |
| `Jwt:Key` | `Program.cs` + `TokenService` | Forja de tokens con rol `Administrador` |

Verificado en este entorno:

- Los 7 commits `0c4450a → a57c995` contienen los tres valores.
- Los tres valores coinciden **exactamente** con lo que está en tu `user-secrets` local, es decir, la BD real se está operando con la clave filtrada.
- `.gitignore` ya lista `setup.ps1` / `setup.cmd` (líneas 489-490) pero eso **no** los desversiona: siguen tracked.
- `Frontend/.env` no está versionado. `appsettings.json` no tiene secretos (solo host/usuario).

**Conclusión clave:** reescribir el historial NO invalida los secretos. La rotación es obligatoria y va primero.

---

## 1. Qué se implementó en el código (ya está, sin commitear)

| Archivo | Cambio |
|---|---|
| `Services/IEncryptionService.cs` | `+ ComputeLookupCandidates`, `+ TryDecryptWithPrimaryKey` (aditivo, no rompe implementaciones) |
| `Services/EncryptionService.cs` | Clave primaria `Encryption:Key` + lista `Encryption:PreviousKeys` con fallback en `Decrypt`; derivación HKDF idéntica (mismo `salt`/`info`) para que el ciphertext viejo siga siendo legible |
| `Data/EncryptionRotationRunner.cs` | Nuevo. Respaldo JSON → re-cifrado por lotes transaccionales → verificación estricta |
| `Program.cs` | Hook `Encryption:RotateOnStartup`; `OnTokenValidated` (revisa `Active` en BD); rate limiter `"auth"`; `UseRouting`/`UseRateLimiter` |
| `Services/AuthService.cs` | Login y registro buscan con `ComputeLookupCandidates` (`IN`) |
| `Services/UserService.cs` | Chequeo de correo duplicado con candidatos |
| `Controllers/AuthController.cs` | `[EnableRateLimiting("auth")]` |
| `appsettings.json` | `Encryption:PreviousKeys: []`, `RotateOnStartup: false`, `RateLimiting:Auth:PermitLimit: 30` |
| `setup.ps1` / `setup.cmd` | Eliminados del árbol de trabajo |

**Por qué el fallback de `EmailHash` no necesita migración de esquema:** `Users.EmailHash` tiene índice único, así que no se pueden guardar dos hashes por fila. En vez de duplicar la columna, el login calcula el hash con la clave nueva **y** con la clave vieja y consulta `WHERE "EmailHash" IN (...)`. Las escrituras nuevas siempre usan la clave primaria. Durante la ventana de transición conviven filas con hash viejo y nuevo sin colisionar.

**Cómo detecta la rotación qué filas faltan:** `ComputeLookup(Decrypt(Email)) == EmailHash`. Si coincide, la fila ya está en la clave nueva. No hace falta columna de versión → **cero migraciones**.

`OnTokenValidated` no rompe funcionalidad: solo consulta `SELECT "Active" FROM "Users" WHERE "Id" = …` (`AsNoTracking`) y hace `context.Fail()` si el usuario no existe o está inactivo. Coste: 1 query por request autenticado. Si después molesta, se cachea 60 s con `IMemoryCache`.

El rate limiter es 30/min por IP sobre `/api/auth/*`. En desarrollo todo llega desde `localhost`, así que 30/min de logins es holgado; si ves `429`, sube `RateLimiting:Auth:PermitLimit`.

Validado: `dotnet build` sin errores; prueba de cifrado con dos claves (descifra heredado, `TryDecryptWithPrimaryKey` = `false` en legado, candidatos contienen el hash viejo, recifrado legible solo con la nueva, sin `PreviousKeys` lanza `CryptographicException`); y el `filter-repo` de la Fase D ejecutado en un espejo local de prueba: 22 commits conservados, `setup.*` fuera de todo el historial y cero apariciones de los tres secretos.

---

## 2. Checklist de HOY, en orden

El orden importa. No saltes pasos.

### Fase A — Contención (30 min, sin tocar código)

- [ ] **A1. Crear rama en Neon antes de tocar la contraseña.** Console de Neon → *Branches* → *Create* → nombre `pre-rotacion`. Es tu rollback: cambiar el host de la rama te devuelve la BD tal como estaba hace un minuto, sin `pg_dump` local (no tienes `psql` instalado).
      Guarda el connection string de la rama, es distinto del de producción.
- [ ] **A2. Rotar la contraseña de Neon.** Console → *Roles & Databases* → `neondb_owner` → *Reset password*. Copia la nueva. **Anota la anterior**, la necesitas para `Encryption:PreviousKeys` y para el restore de la rama.
      Esta es la acción de mayor severidad: la contraseña daba acceso total a la BD.
- [ ] **A3. Actualizar tu `user-secrets` con la contraseña nueva** y dejar la vieja a mano en un gestor de contraseñas, no en un archivo del repo:
      ```powershell
      dotnet user-secrets set "Database:Password" "<NUEVA>" --project src/Backend.API
      ```
- [ ] **A4. Generar las claves nuevas** (aún no las apliques):
      ```powershell
      $newJwt  = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
      $newEnc  = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
      $newJwt; $newEnc
      ```
- [ ] **A5. Apuntar la clave de cifrado VIEJA** en un gestor, es imprescindible para el paso C3.

### Fase B — Code (1 h)

- [ ] **B1. Revisar los cambios** ya aplicados en la sección 1 (`git diff`).
- [ ] **B2. Compilar:**
      ```powershell
      dotnet build src\Backend.API\Backend.API.csproj
      ```
      Si `Backend.API.exe` está bloqueado, cierra la API que esté corriendo (`Get-Process Backend.API`) y repite.
- [ ] **B3. Probar el login con la configuración ACTUAL** (aún sin rotar nada): la API debe arrancar y hacer login igual que antes. Esto confirma que `OnTokenValidated` y el rate limiter no rompieron nada.
- [ ] **B4. Commit.** Necesario antes del force-push, porque después harás `reset --hard` y perderías lo no commiteado:
      ```powershell
      git add -A
      git commit -m "Endurece auth: cifrado dual-key con fallback, OnTokenValidated, rate limit en /api/auth y elimina setup.* con secretos"
      ```

### Fase C — Rotación de cifrado (45 min, con la API parada)

- [ ] **C1. Dejar la `Encryption:Key` vieja como `PreviousKeys[0]` y poner la nueva como primaria:**
      ```powershell
      dotnet user-secrets set "Encryption:Key" "<NUEVA>" --project src/Backend.API
      dotnet user-secrets set "Encryption:PreviousKeys:0" "<VIEJA>" --project src/Backend.API
      ```
      A partir de aquí la API arranca con **ambas** claves: los datos viejos se leen, los nuevos se escriben con la nueva.
- [ ] **C2. Reiniciar la API en modo rotación** y dejar que termine sola:
      ```powershell
      $env:Encryption__RotateOnStartup = "true"
      dotnet run --project src\Backend.API
      ```
      Qué hace, en orden: `Migrate()` → respaldo JSON → re-cifra `Email`, `Phone` y recalcula `EmailHash` en lotes de 50, cada lote en su transacción → verificación estricta.
      **Si sale con excepción, la API no levanta.** Es intencional: primero arreglas, luego sigues.
- [ ] **C3. Revisar el log.** Debe decir `fallidos=0` y `verificados=` igual al total de usuarios.
      El respaldo queda en `src\Backend.API\bin\Debug\net10.0\encryption-backup\encryption-rotation-*.json`. Consérvalo hasta terminar la Fase E.
- [ ] **C4. Segunda ejecución en seco** (idempotencia): vuelve a lanzarlo con la misma variable. Debe reportar `rotados=0` y `ya vigentes=<total>`. Si rota algo, algo está mal.
- [ ] **C5. Validar contra la rama de Neon.** Aplica la rama `pre-rotacion` a la API (cambia `Database:Host`, `Port`, `User`, `Password`) y haz login. Si funciona contra el respaldo, la BD real está bien.
- [ ] **C6. Apagar el modo rotación:**
      ```powershell
      $env:Encryption__RotateOnStartup = "false"
      ```
- [ ] **C7. Rotar `Jwt:Key` ahora** (es lo más simple: solo invalida sesiones):
      ```powershell
      dotnet user-secrets set "Jwt:Key" "<NUEVA>" --project src/Backend.API
      ```
      Todos los tokens emitidos con la clave vieja mueren. Nadie pierde datos, solo tiene que hacer login otra vez.

### Fase D — Limpieza del historial (1 h, lo más delicado)

> **Nunca** ejecutes esto en `C:\Users\xXSIL\Source\APP_Mantenimiento_Equipos`. Hazlo en un clon espejo aparte. `filter-repo` borra `origin` y reescribe todo; un error aquí no tiene `undo`.

- [ ] **D1. Instalar `git-filter-repo`:**
      ```powershell
      python -m pip install --user git-filter-repo
      git filter-repo --version   # debe responder un número
      ```
      Si `git filter-repo` no se reconoce (típico en Windows), agrega la carpeta de scripts al PATH. No uses `site.USER_BASE`: apunta un nivel de directorios demasiado arriba y el comando sigue sin aparecer.
      ```powershell
      $env:Path += ";$(python -c "import sysconfig; print(sysconfig.get_path('scripts','nt_user'))")"
      git filter-repo --version
      ```
      Alternativa sin tocar el PATH: `python -m git_filter_repo --version`.
- [ ] **D2. Clon espejo, sin `--single-branch`, a un directorio fuera del repo:**
      ```powershell
      $mirror = "$env:TEMP\mp-mirror"
      git clone --mirror https://github.com/alejandrabetancour225-design/Sistema-de-mantenimiento-de-equipos.git $mirror
      ```
- [ ] **D3. Archivo de sustitución para los literales.** Créalo **dentro de `%TEMP%`**, nunca en el repo, y bórralo al terminar. Usa `-Encoding ASCII`: con `UTF8`, PowerShell 5.1 mete un BOM y la primera línea (`literal:`) deja de reconocerse, así que la clave JWT no se sustituiría sin avisar:
      ```powershell
      $replaceFile = "$env:TEMP\mp-replace.txt"
      @(
        'literal:ZJfV/E5RCxSYXDh8YE54fus6PRr20bYKj7NxUV6EvRP9WTeDcdPKVeoHyoRequcMxmj7Q/EM6h/sxHIY3RBlhQ==>***JWT_KEY_REMOVED***'
        'literal:6rFo7LNX5I95pY4AiEJt7PVsaazsJOcSA1LMPyPLi+E==>***ENC_KEY_REMOVED***'
        'literal:npg_iFWwZA9cEbm6==>***NEON_PASSWORD_REMOVED***'
      ) | Set-Content -LiteralPath $replaceFile -Encoding ASCII
      ```
- [ ] **D4. Reescribir en el espejo:**
      ```powershell
      git -C $mirror filter-repo --force `
        --path src/Backend.API/setup.ps1 `
        --path src/Backend.API/setup.cmd `
        --invert-paths `
        --replace-text $replaceFile
      ```
      `--invert-paths` **borra** esos dos archivos de todos los commits. `--replace-text` limpia los literales por si aparecieran en otro archivo (README, blobs viejos).
      En PowerShell las comillas simples y `>` no se interpretan: el contenido lleva `==` como separador, no `=>`.
- [ ] **D5. `filter-repo` borra el remote `origin` al terminar** (es su comportamiento por defecto para evitar empujar a un upstream por error). Con un clon desde GitHub lo vas a perder; con un clon desde una ruta local lo conserva. Restáuralo antes del push. `set-url` funciona en ambos casos, así que no necesitas comprobar nada:
      ```powershell
      git -C $mirror remote set-url origin https://github.com/alejandrabetancour225-design/Sistema-de-mantenimiento-de-equipos.git
      git -C $mirror remote -v
      ```
- [ ] **D6. Verificar antes de empujar:**
      ```powershell
      git -C $mirror rev-list --all --count                        # mismos commits que tenías
      git -C $mirror log --all --name-only --format= | Select-String 'setup\.ps1|setup\.cmd'   # sin salida = OK
      git -C $mirror log -p --all | Select-String 'npg_|ZJfV|6rFo7LNX'                          # sin salida = OK
      git -C $mirror log --oneline -3
      ```
      Si el segundo comando todavía encuentra `setup.*`, repite D4 con las rutas exactas que imprima (`git -C $mirror log --all --name-only --format=`).
      Si el tercero encuentra algo, el `--replace-text` no se aplicó (revisa el BOM de D3) y hay que rehacerlo desde D2.
- [ ] **D7. Force-push.** Revisa antes en GitHub *Settings → Branches* si `master` tiene protección de push; si la tiene, desactívala, empuja y reactívala.
      ```powershell
      git -C $mirror push --force --mirror origin
      ```
      A partir de este momento todos los clones del mundo tienen que re-clonar.
- [ ] **D8. Sincronizar tu clon de trabajo** (los cambios de la Fase B ya están commiteados en B4, no se pierden):
      ```powershell
      git fetch origin
      git reset --hard origin/master
      ```
- [ ] **D9. Borrar rastros locales y el archivo de literales:**
      ```powershell
      Remove-Item -LiteralPath $replaceFile -Force
      Remove-Item -LiteralPath $mirror -Recurse -Force
      git reflog expire --expire=now --all
      git gc --prune=now --aggressive
      ```
- [ ] **D10. En GitHub:** *Settings → Danger Zone*, revisa si hay forks o PRs que guarden el archivo viejo. Si los hay, el secreto sigue expuesto → por eso la Fase A es obligatoria aunque el historial quede limpio. Deja constancia de la rotación en el README del equipo.

### Fase E — Equipo y cierre (30 min)

- [ ] **E1. Avisar al equipo:** el historial se reescribió, todos deben re-clonar (`git clone` de nuevo, no `pull`), y unificar `Encryption:Key` y `Jwt:Key` por canal seguro. Los secretos de `setup.ps1` están quemados, nadie debe volver a pegarlos en un archivo.
- [ ] **E2. Cada dev actualiza sus secretos:**
      ```powershell
      dotnet user-secrets set "Database:Password" "<NUEVA>" --project src/Backend.API
      dotnet user-secrets set "Jwt:Key"        "<NUEVA>" --project src/Backend.API
      dotnet user-secrets set "Encryption:Key" "<NUEVA>" --project src/Backend.API
      ```
      Si alguien no actualiza `Encryption:Key`, no podrá leer los correos recién cifrados. Es el motivo del paso E3.
- [ ] **E3. Plan B para desincronizados:** mientras `Encryption:PreviousKeys[0]` siga configurada, la API lee datos con cualquiera de las dos claves. **No la retires el mismo día.** Revisa el log en busca de `CryptographicException`; cuando haya 0 errores durante una semana, elimínala:
      ```powershell
      dotnet user-secrets remove "Encryption:PreviousKeys:0" --project src/Backend.API
      ```
      Retirarla antes de tiempo deja usuarios con correos indescifrables.
- [ ] **E4. Borrar los respaldos de rotación** cuando la verificación de la Fase F esté verde. Contienen PII cifrada, no los dejes en el repo ni en `bin`.

---

## 3. Verificación post-rotación

### 3.1 Claves y configuración

```powershell
# La clave vieja debe estar SOLO en PreviousKeys, nunca en Encryption:Key
dotnet user-secrets list --project src\Backend.API
```
- [ ] `Encryption:Key` = valor nuevo, distinto del filtrado.
- [ ] `Encryption:PreviousKeys:0` = valor viejo (temporal).
- [ ] `Database:Password` = contraseña nueva de Neon; **la vieja ya no sirve** → confírmalo intentando login en la BD con ella, debe fallar.

### 3.2 La API no filtra ni un secreto

```powershell
dotnet run --project src\Backend.API 2>&1 | Tee-Object api.log
Select-String -Path api.log -Pattern 'npg_|ZJfV|6rFo7LNX|password|Password='
```
- [ ] Sin coincidencias. `Program.cs:20` arma el connection string por concatenación: asegúrate de que `EnableSensitiveDataLogging` esté en `false` (ya lo está por defecto) y no actives el logging de EF en producción.

### 3.3 Datos descifrados ( funcionalidad intacta)

```powershell
# Login real por HTTP
$body = @{ email = "<correo-admin-real>"; password = "<password-real>" } | ConvertTo-Json
$r = Invoke-RestMethod -Uri "http://localhost:5255/api/auth/login" -Method Post -Body $body -ContentType "application/json"
$r.token.Length; $r.email; $r.role
```
- [ ] Devuelve token y el **correo en claro correcto** → prueba de que el re-cifrado funcionó de extremo a extremo.
- [ ] `GET /api/users` con ese token devuelve los correos y teléfonos de todos los usuarios, sin correos vacíos ni base64 en crudo (lo que verías si la API te devolviera el ciphertext):
      ```powershell
      $u = Invoke-RestMethod -Uri "http://localhost:5255/api/users" -Headers @{ Authorization = "Bearer $($r.token)" }
      $u.Count
      $u | Where-Object { $_.email -notmatch '^[^@]+@[^@]+\.[^@]+$' }   # sin salida = todos legibles
      ```
- [ ] `$u.Count` coincide con el número que reportó `revisados=` en la Fase C3.

### 3.4 `OnTokenValidated` funciona (y no rompe)

- [ ] Con token válido → `200` en `GET /api/users`.
- [ ] Desactiva un usuario con `PUT /api/users/{id}` y `{"active": false}`, y repite `GET /api/users` con **ese mismo** token → ahora debe dar `401`; antes de este cambio habría dado `200`. Reactívalo en seguida para no dejar la cuenta muerta.
- [ ] Un token de un usuario borrado también da `401` (no `500`): es la rama `active is null` del handler.

### 3.5 Rate limiting

```powershell
1..35 | ForEach-Object {
  try { Invoke-WebRequest -Uri "http://localhost:5255/api/auth/login" -Method Post -Body '{}' -ContentType 'application/json' -ErrorAction Stop | Out-Null; "." }
  catch { $_.Exception.Response.StatusCode.value__ }
}
```
- [ ] A partir del request 31 aparece `429`. Confirma que el límite es el que crees y no un falso positivo de otro error.
- [ ] `GET /api/openapi/v1.json` (solo en Development) sigue en `200`: el limiter no se comió endpoints que no son de auth.

### 3.6 Rotación idempotente y respaldo

- [ ] Repetir C4 reporta `rotados=0`.
- [ ] El JSON de respaldo de C3 existe y tiene tantas entradas como usuarios.
- [ ] Clonar la rama `pre-rotacion` de Neon y hacer login contra ella funciona (C5). Con eso ya tienes un rollback probado.

### 3.7 Historial limpio

```powershell
git log --all -p | Select-String 'npg_|ZJfV|6rFo7LNX'
git log --all --name-only --format= | Select-String 'setup\.'
```
- [ ] Ambos sin salida.
- [ ] `git log --oneline | Measure-Object -Line` da los mismos commits que antes de la reescritura (solo cambian los hashes).

---

## 4. Lo que NO se hizo a propósito

| Deuda | Por qué se dejó | Cuándo |
|---|---|---|
| `/api/auth/register` abierto: el primer usuario que se registra sin admin existente se vuelve `Administrador` | Endurecerlo cambia el flujo de alta del equipo | Cuando haya alta por invitación o el primer admin esté garanciado |
| `LoginAsync` no hace PBKDF2 ficticio cuando el correo no existe | Oráculo de tiempos para enumerar correos | Con el rate limiter ya puesto, el riesgo baja mucho |
| `OnTokenValidated` consulta la BD en cada request | Añade latencia | Cachear 60 s si el perfil de carga lo justifica |
| Sin CSP ni `X-Content-Type-Options` | El frontend es el único consumidor y no hay input de usuario en la API | Si se sirve HTML, montar el helmet |
| Sin test automatizado del round-trip de cifrado | No hay proyecto de tests en la solución | Si se crea, copiar la prueba de `Fase 1` §1 |

## 5. Si algo sale mal

| Síntoma | Causa probable | Acción |
|---|---|---|
| `CryptographicException` al listar usuarios | Alguien quitó `PreviousKeys` antes de tiempo, o un dev quedó desincronizado | Restaura `Encryption:PreviousKeys:0` y reinicia. Si un dev tiene la clave nueva pero el repo exige la vieja, coordina por canal seguro |
| La API no arranca tras la rotación | `fallidos>0` o `verificados<revisados` | Restaura el respaldo JSON y repite C2. No sigas adelante con datos a medias |
| `Cannot configure a Microsoft.Extensions.FileProviders.FileProvider` / login responde `500` al listar | Key mal formada en `PreviousKeys` (no Base64 o <32 bytes) | Revalida: debe ser Base64 de 32 bytes exactos |
| `429` en desarrollo | Límite de 30/min excedido | Sube `RateLimiting:Auth:PermitLimit` en `appsettings.json` |
| Login `401` con credenciales correctas | `Encryption:Key` nueva pero filas aún con hash viejo **y** sin `PreviousKeys` | Restaurar `PreviousKeys:0` y repetir C2 |
| El force-push fue rechazado | Protección de branch en GitHub | Desactiva *Settings → Branches → Protect* para `master`, reintenta, reactívalo |
