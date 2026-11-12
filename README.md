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

**Usuarios de demostración** (con `--demo`, en la "Organización por defecto"): `empleado1@demo.local`, `empleado2@demo.local`, `tecnico1@demo.local`, `gerente1@demo.local`, todos con la contraseña `DemoSprint6x2026`.

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

- [verificacion/fase0.sh](verificacion/fase0.sh): 26 comprobaciones de reglas de negocio (fechas, corrección de datos, estados, permisos por rol).
- [verificacion/fase3.sh](verificacion/fase3.sh): 19 comprobaciones de multiempresa (aislamiento, contraseña temporal, organizaciones inactivas).
- [verificacion/fase4.sh](verificacion/fase4.sh): 19 comprobaciones de alquiler (contratos, tarifas, reclamos, devolución, reemplazo).
- [verificacion/fase5.sh](verificacion/fase5.sh): 12 comprobaciones de calendario, recordatorios y SignalR (usa Node).

Las fases 3 a 5 necesitan `SUPERADMIN_EMAIL` y `SUPERADMIN_PASSWORD` y crean su propia organización de prueba.

Crean datos con prefijo `QA-S6`. Para borrar los de `fase0.sh`, ejecuta [verificacion/limpiar-qa.sql](verificacion/limpiar-qa.sql) en la consola SQL de Neon.

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

### Multiempresa y planes

- Cada **organización** (empresa) ve solo sus propios datos; el filtro lo aplica la base de datos en cada consulta.
- El **SuperAdmin** de la plataforma crea organizaciones, les asigna plan y crea su primer Administrador. No ve los datos de las empresas. Se crea al arrancar con `BOOTSTRAP_SUPERADMIN_EMAIL` / `BOOTSTRAP_SUPERADMIN_PASSWORD` del `.env`.
- **Planes** (asignados a mano, sin cobros): **Básico** (todo lo operativo + diagnóstico con IA) e **Integraciones** (además: bases de datos de terceros, minería de datos, reportes generados por IA e información de mercado).
- No hay registro público: el **Administrador** de cada empresa crea sus usuarios con una **contraseña temporal** que deben cambiar al entrar (la API no deja hacer nada más hasta cambiarla).
- Los datos anteriores al Sprint 6 quedaron en la **"Organización por defecto"** (plan Integraciones).
- El rol *Cliente* ahora se llama **Gerente**.

### Alquiler de equipos

- **Arrendadores** y **contratos** con tarifa mensual (COP) por tipo de equipo. Un equipo puede ser **propio** o **alquilado**; el alquilado toma como fecha de adquisición el inicio del contrato.
- Las fallas de un equipo alquilado se reportan al arrendador con un **reclamo** (le llega por correo). El mantenimiento preventivo interno solo si el contrato lo permite.
- Un alquilado no se da de baja: se **devuelve** (estado terminal), solo o con todo el contrato. El arrendador puede enviar un **equipo de reemplazo**.

### Calendario, recordatorios y tiempo real

- **Planes preventivos** por equipo cada N semanas. Al cerrar un preventivo, la próxima fecha se calcula sola. En el **calendario** se arrastra la fecha o se inicia el mantenimiento.
- **Recordatorios** 7 días y 1 día antes, y al vencer, para el técnico responsable, los administradores y el empleado que tiene el equipo; además, aviso de contratos de alquiler 30 días antes de vencer. Llegan a la **campana** y por **correo** (si `SMTP_HOST` está configurado; si no, el correo queda en el log de la API).
- La tarea corre cada hora (minuto 15, Hangfire, guardada en Postgres en el esquema `hangfire`) y al arrancar. El Administrador puede forzarla con `POST /api/notifications/RunReminders`.
- **Tiempo real (SignalR)**: cuando alguien de la empresa modifica datos, dashboard, calendario y listas se refrescan solos.

### Integraciones con bases de datos de terceros (plan Integraciones)

- Fuentes: **archivo** (CSV, Excel .xlsx o JSON, hasta 5 MB) o **conexión en vivo** a PostgreSQL, MySQL/MariaDB, SQL Server o MongoDB.
- **Solo lectura**: las sesiones SQL se abren en modo de solo lectura, solo se ejecuta un `SELECT` sobre una tabla con nombre validado y se recomienda a la empresa crear un usuario de solo lectura. Nunca se escribe en la fuente.
- `python-service` **perfila** los datos (tipos, vacíos, unicidad, ejemplos) y **propone el mapeo** hacia el modelo de equipo según el nombre y el contenido de cada columna (entiende formatos colombianos: fechas dd/mm/aaaa y precios 1.234.567). El Administrador lo confirma.
- Los equipos importados quedan ligados a su fuente: los campos mapeados **no se editan** en la app (se corrigen en la fuente) y las columnas sin equivalente se guardan en **JSONB** (`ExternalAttributes`). Sincronización manual o cada N horas; una segunda sincronización actualiza, no duplica.
- Decisión de diseño (Q4): la opción (a), JSONB dentro del mismo PostgreSQL, resolvió el caso; no hizo falta MongoDB ni crear tablas dinámicas.
- Probado con CSV, Excel, JSON y PostgreSQL ([verificacion/fase6.sh](verificacion/fase6.sh), base de prueba en [verificacion/base-externa.sql](verificacion/base-externa.sql)). Los conectores de MySQL, SQL Server y MongoDB están instalados pero **no se probaron contra un servidor real**.

### Roles y permisos (se aplican en la API, no solo en la interfaz)

| Módulo | Administrador | Técnico | Empleado | Gerente |
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
