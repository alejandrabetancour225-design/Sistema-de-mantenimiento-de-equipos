# Sistema de Mantenimiento de Equipos

Inventario, asignación, mantenimiento e historial de equipos, con diagnóstico asistido por IA local.

| Parte | Tecnología | Carpeta |
| --- | --- | --- |
| API | .NET 10 + EF Core + PostgreSQL | [src/Backend.API](src/Backend.API) |
| Frontend | React 19 + Vite + TypeScript | [Frontend](Frontend) |
| IA | Python (FastAPI) + Ollama, solo CPU | [python-service](python-service) |
| Base de datos | PostgreSQL en Neon (compartida); Postgres local opcional | — |

---

## Arrancar el proyecto (un solo comando)

### 1. Instala Docker

- **Windows / macOS**: [Docker Desktop](https://www.docker.com/products/docker-desktop/). Ábrelo antes de seguir.
- **Linux**: Docker Engine o Podman con `podman-compose`.

Nada más es obligatorio: .NET y Node solo hacen falta para programar con recarga en caliente (modo desarrollo).

Equipo recomendado: **8 GB de RAM como mínimo, 16 GB ideal**. No necesita GPU.

### 2. Crea el archivo `.env`

```bash
cp .env.example .env        # en Windows: copy .env.example .env
```

Completa `DB_PASSWORD`, `JWT_KEY` y `ENCRYPTION_KEY` con los valores que te pase **el líder del equipo por un canal privado** (WhatsApp, Drive privado…). Nunca los subas a git ni los pegues en archivos del repositorio.

> Todo el equipo usa **los mismos** valores porque comparte la base de Neon. Con otra `ENCRYPTION_KEY` no se pueden leer los correos de los usuarios.

### 3. Ejecuta

| Sistema | Comando |
| --- | --- |
| Linux / macOS | `./iniciar.sh` |
| Windows | Doble clic en **`iniciar.cmd`** (o `.\iniciar.ps1` en PowerShell) |

El script:

1. Comprueba Docker/Podman y el `.env`.
2. La primera vez pregunta si quieres **datos de demostración**.
3. Levanta Ollama y descarga el modelo de IA si no está (solo la primera vez, ~5 GB; muestra el progreso).
4. Construye y levanta todo, aplica las migraciones y abre **http://localhost:8080**.

### Opciones

| Linux / macOS | Windows | Qué hace |
| --- | --- | --- |
| `./iniciar.sh --dev` | `iniciar.cmd -Dev` | Modo desarrollo: la IA en contenedores y la API (`dotnet watch`) y el frontend (Vite) **con recarga en caliente** en http://localhost:5173. Necesita .NET 10 SDK y Node 22. Ctrl+C detiene ambos. |
| `./iniciar.sh --db-local` | `iniciar.cmd -DbLocal` | Usa un Postgres local en vez de Neon (para trabajar sin internet). Se combina con `--dev`. |
| `./iniciar.sh --demo` | `iniciar.cmd -Demo` | Carga los datos de demostración (no los duplica si ya existen). |
| `./iniciar.sh --detener` | `iniciar.cmd -Detener` | Detiene todos los contenedores. |

**Usuarios de demostración** (con `--demo`): `empleado1@demo.local`, `empleado2@demo.local`, `tecnico1@demo.local`, `gerente1@demo.local`, todos con la contraseña `DemoSprint6x2026`.

### Antes de arrancar: `git pull`

La base de Neon es compartida y **las migraciones se aplican solas al arrancar** (solo hacia adelante, nunca borran). Si alguien ya migró la base con código más nuevo y tú arrancas una versión vieja, tu API puede fallar. Haz `git pull` antes de `iniciar`.

### Problemas frecuentes

| Síntoma | Solución |
| --- | --- |
| "No encontré Docker ni Podman" | Abre Docker Desktop y espera a que diga *running*. |
| "Faltan valores en .env" | Pide los valores al líder del equipo. |
| La IA tarda | En CPU un diagnóstico puede tardar de 1 a 3 minutos. Cierra programas pesados. |
| El puerto 8080 está ocupado | Cambia `APP_PORT` en el `.env`. |
| Ver qué pasa en la API | `docker compose logs -f backend` |
| Windows no deja ejecutar `.ps1` | Usa `iniciar.cmd`, que ya lo ejecuta con permisos. |

---

## Desarrollo sin el script

Si prefieres levantar cada parte a mano:

```bash
docker compose up -d ollama python-service          # IA
# La API lee la configuración de variables de entorno (las mismas del .env, con el formato Database__Password=...).
dotnet watch --project src/Backend.API run --urls http://localhost:5255
npm --prefix Frontend run dev                      # http://localhost:5173 (reenvía /api a :5255)
```

El frontend siempre llama a `/api`: en contenedores lo reenvía nginx y en desarrollo el proxy de Vite. Si tienes un `Frontend/.env.local` viejo con `VITE_API_URL=http://localhost:5255/api`, puedes borrarlo.

### Migraciones

Se aplican solas al arrancar la API. Para crear una nueva:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add NombreDeLaMigracion --project src/Backend.API
```

### Verificación manual

[verificacion/fase0.sh](verificacion/fase0.sh) ejecuta 26 comprobaciones de las reglas de negocio contra la API en marcha (fechas, corrección de datos, estados, permisos por rol). Crea datos con prefijo `QA-S6`. Para borrarlos después, ejecuta [verificacion/limpiar-qa.sql](verificacion/limpiar-qa.sql) en la consola SQL de Neon.

```bash
ADMIN_EMAIL=... ADMIN_PASSWORD=... ./verificacion/fase0.sh
```

---

## Reglas de negocio principales

### Estados del equipo (R4: una sola regla maestra)

Todos los cambios de estado pasan por [EquipmentStateMachine](src/Backend.API/Services/EquipmentStateMachine.cs):

| Acción | De | A |
| --- | --- | --- |
| Asignar | Disponible | En uso |
| Liberar | En uso | Disponible |
| Abrir mantenimiento | Disponible, En uso, Fuera de servicio | En mantenimiento |
| Cerrar mantenimiento | En mantenimiento | Vuelve a servicio (En uso si tiene asignación, si no Disponible) o Fuera de servicio |
| Cancelar mantenimiento | En mantenimiento | Vuelve a servicio |
| Manual | Disponible / En uso ⇄ Fuera de servicio | |
| Dar de baja (solo Administrador) | Disponible, Fuera de servicio | Dado de baja (**terminal**) |

### Datos del equipo

- La **fecha de adquisición no puede ser futura** (fecha de Colombia) y la **garantía no puede terminar antes de la compra**. Se valida en la API y en el formulario.
- Código interno y número de serie son únicos (sin distinguir mayúsculas; se guardan en mayúsculas). Solo un **Administrador** puede corregirlos.
- Cualquier dato se puede corregir, incluso vaciar fechas o precio. La auditoría guarda **valor anterior y nuevo**.

### Mantenimientos

- Un mantenimiento cerrado no se edita. Si tiene errores, el **Administrador lo anula** (queda en el historial con el motivo, fuera de costos y reportes) y se registra uno nuevo.
- Solo uno abierto por equipo. Completar exige el trabajo realizado.

### Roles y permisos (se aplican en la API, no solo en la interfaz)

| Módulo | Administrador | Técnico | Empleado | Cliente/Gerente |
| --- | --- | --- | --- | --- |
| Usuarios, auditoría | Todo | — | — | — |
| Equipos | Todo | Crear, editar, cambiar estado | Solo los que tiene asignados | — |
| Dar de baja, corregir código/serie, anular mantenimientos | Sí | — | — | — |
| Asignaciones | Todo | Todo | Solo las suyas | — |
| Incidentes | Todo | Consulta | Reportar sobre sus equipos | — |
| Mantenimientos e historial | Todo | Todo | Solo de sus equipos | Consulta |
| Biblioteca de errores (IA) | Todo | Todo | Consulta | — |
| Dashboard | Gerencial | Operativo | Operativo (sus equipos) | Gerencial |
| Reportes | Sí | — | — | Sí |

---

## Seguridad

- **Secretos**: solo en el `.env` (ignorado por git). [.env.example](.env.example) documenta cada variable sin valores reales.
- **Contraseñas**: 10 a 128 caracteres con letras y números; PBKDF2-SHA256 (600.000 iteraciones); bloqueo de 15 minutos tras 5 intentos fallidos.
- **Sesión**: JWT. Cambiar el rol, desactivar o cerrar sesión invalida los tokens anteriores.
- **Cifrado**: correo y teléfono con AES-256-GCM; búsqueda por correo con un índice ciego (HMAC).
- **Puertos**: todos los servicios escuchan solo en `127.0.0.1` (no quedan expuestos a la red).
- **Límites**: 300 peticiones/minuto por usuario, 10 logins/minuto por IP, cuerpo de 1 MB.

### Rotar claves

- **`JWT_KEY`**: cámbiala en el `.env` de todos y reinicia. Todos deben volver a iniciar sesión.
- **`ENCRYPTION_KEY`**: hay que recifrar los datos. Ejecuta la API una vez con `Encryption__Key=<nueva>`, `Encryption__PreviousKeys__0=<vieja>` y `Encryption__RotateOnStartup=true`. La API recifra todo, lo verifica, deja un respaldo y se detiene. Después reparte el `.env` nuevo al equipo.
- **`DB_PASSWORD`**: se cambia en el panel de Neon (*Roles → Reset password*) y se reparte el `.env` nuevo.

## Documentación de la API

- OpenAPI (modo desarrollo): http://localhost:5255/openapi/v1.json
- Peticiones de ejemplo: [src/Backend.API/Backend.API.http](src/Backend.API/Backend.API.http)
