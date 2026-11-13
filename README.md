# Sistema de Mantenimiento de Equipos

Inventario, asignación, mantenimiento e historial de equipos, con diagnóstico asistido por IA local.

| Parte | Tecnología | Carpeta |
| --- | --- | --- |
| API | .NET 10 + EF Core + PostgreSQL | [src/Backend.API](src/Backend.API) |
| Frontend | React 19 + Vite + TypeScript | [Frontend](Frontend) |
| IA | Python (FastAPI) + Ollama, solo CPU | [python-service](python-service) |
| Base de datos | PostgreSQL en Neon (compartida); Postgres local opcional | — |

---

## Arrancar el proyecto

### 1. Instala Docker

- **Windows / macOS**: [Docker Desktop](https://www.docker.com/products/docker-desktop/). Ábrelo antes de seguir.
- **Linux**: Docker Engine o Podman con `podman-compose`.

Nada más es obligatorio: .NET y Node solo hacen falta para programar con recarga en caliente (modo desarrollo).

Equipo recomendado: **8 GB de RAM como mínimo, 16 GB ideal**. No necesita GPU.

### 2. Crea el archivo `.env`

```bash
cp .env.example .env        # en Windows: copy .env.example .env
```

Completa `DB_PASSWORD`, `JWT_KEY` y `ENCRYPTION_KEY` con los valores (privados) que vayas a utilizar.

> Todo el equipo debe usar los mismos valores porque comparte la base de Neon. Con otra `ENCRYPTION_KEY` no se pueden leer los correos de los usuarios.

### 3. Ejecuta

| Sistema | Comando |
| --- | --- |
| Linux / macOS | `./iniciar.sh` |
| Windows | Doble clic en **`iniciar.cmd`** (o `.\iniciar.ps1` en PowerShell) |

El script:

1. Comprueba Docker/Podman y el `.env`.
2. La primera vez pregunta si quieres **datos de demostración**.
3. Levanta Ollama y descarga el modelo de IA si no está (solo la primera vez, ~2 GB; muestra el progreso).
4. Construye y levanta todo, aplica las migraciones y abre **http://localhost:8080**.

### Opciones

| Linux / macOS | Windows | Qué hace |
| --- | --- | --- |
| `./iniciar.sh --dev` | `iniciar.cmd -Dev` | Modo desarrollo: la IA en contenedores y la API (`dotnet watch`) y el frontend (Vite) **con recarga en caliente** en http://localhost:5173. Necesita .NET 10 SDK y Node 22. Ctrl+C detiene ambos. |
| `./iniciar.sh --db-local` | `iniciar.cmd -DbLocal` | Usa un Postgres local en vez de Neon. Se combina con `--dev`. |
| `./iniciar.sh --demo` | `iniciar.cmd -Demo` | Carga los datos de demostración (no los duplica si ya existen). |
| `./iniciar.sh --detener` | `iniciar.cmd -Detener` | Detiene todos los contenedores. |

**Usuarios de demostración** (con `--demo`, en la "Organización por defecto"): `empleado1@demo.local`, `empleado2@demo.local`, `tecnico1@demo.local`, `gerente1@demo.local`, todos con la contraseña `DemoSprint6x2026`.

### Antes de arrancar: `git pull`

La base de Neon es compartida y **las migraciones se aplican solas al arrancar** (solo hacia adelante, nunca borran). Si alguien ya migró la base con código más nuevo y tú arrancas una versión vieja, tu API puede fallar. Haz `git pull` antes de `iniciar`.

---

## Desarrollo sin el script

Si prefieres levantar cada parte a mano:

```bash
docker compose up -d ollama python-service  # contenedor de IA

# La API lee la configuración de variables de entorno
dotnet watch --project src/Backend.API run --urls http://localhost:5255
npm --prefix Frontend run dev # http://localhost:5173 (reenvía /api a :5255)
```

El frontend siempre llama a `/api`: en contenedores lo reenvía nginx y en desarrollo el proxy de Vite.

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

### Estados del equipo

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

### IA en local

- Modelo por defecto **`llama3.2:3b`** (Ollama), elegido con [python-service/eval](python-service/eval/README.md) sobre 24 fallas descritas en lenguaje informal colombiano:

  | Modelo | JSON válido | En español | Causas relevantes | Mediana (4 hilos CPU) | RAM |
  | --- | --- | --- | --- | --- | --- |
  | qwen2.5:3b | 24/24 | 24/24 | 24/24 | 16 s | 1,95 GB |
  | llama3.2:3b | 24/24 | 24/24 | 24/24 | 25 s | 2,17 GB |
  | gemma3:4b | 24/24 | 24/24 | 24/24 | 26 s | 2,69 GB |
  | qwen3.5:4b | 24/24 | 24/24 | 24/24 | 35 s | 2,87 GB |

  `llama3.2:3b` tuvo el mejor criterio técnico con un consumo moderado. Se cambia con `OLLAMA_MODEL` en el `.env`.
- **JSON estricto garantizado**: Ollama recibe el JSON Schema en `format` y restringe la generación con una gramática; el backend C# siempre recibe la forma que espera, con cualquier modelo.
- Ollama limitado a 3,5 GB de RAM (`OLLAMA_MEM_LIMIT`), contexto de 2048 tokens, un pedido a la vez y el modelo sale de la RAM tras 5 minutos sin uso.
- **Lenguaje informal**: glosario de 102 modismos colombianos ("se fundió", "se pega", "echa candela"…) que la IA usa para entender; cada empresa agrega los suyos (Técnico o Administrador).
- **Reportar una falla** (Empleado): chat que entiende el lenguaje cotidiano, hace hasta 3 preguntas y da solo consejos seguros. La incidencia la crea el empleado al confirmar (R5: la IA nunca cambia datos por sí sola). Las causas probables solo las ve el técnico.

### Reportes con IA e información de mercado

- **Reporte ejecutivo** redactado por IA con las cifras del periodo (estados, mantenimientos, costos, fallas repetidas, planes vencidos, alquileres): automático el **día 1 de cada mes** y bajo demanda. Se ve en la app, se descarga en **PDF** y se avisa por campana y correo a Gerentes y Administradores.
- **Información de mercado**: `ddgs` consulta buscadores públicos (Bing, DuckDuckGo, Brave…) y, si fallan, una instancia propia de **SearXNG** (`SEARXNG_URL`). Cada dato muestra su **fuente y fecha**; la IA solo puede citar las fuentes que recibió.
- En el detalle de un equipo: "Buscar precio y guías" (precio de reemplazo en Colombia y guías de mantenimiento/reparación).

### Roles y permisos

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

## Referencia de la API

### Límites

- General: 300 peticiones por minuto por usuario (o por IP sin sesión).
- Inicio de sesión, cambio de contraseña y recuperación (`login`, `change-password`, `forgot-password`, `reset-password`): 10 por minuto por IP.
- Cuerpo de la petición: máximo 1 MB. Excepción: integraciones (`/api/integrations/Profile` y `/api/integrations/Sources`), hasta 8 MB para subir archivos de hasta 5 MB.
- Textos con largo máximo y JSON libre (`characteristics`) de máximo 8.000 caracteres.
- Todos configurables en `appsettings.json` (`RateLimiting`, `Limits`).

### Errores y auditoría

- Los errores no controlados responden `{ "message", "traceId" }` sin detalles internos; el detalle queda en el log con el mismo `traceId`.
- Los errores de validación responden `400` con `{ "message": "...", "errors": { "campo": ["..."] } }`.
- Toda alta, cambio o borrado queda en la tabla `AuditLogs` (usuario, IP, organización, entidad y, en las entidades operativas, **valor anterior y nuevo** de cada campo), igual que los inicios de sesión, fallos y bloqueos. En usuarios solo se registran los nombres de los campos (no se duplican datos personales). Consulta: `GET /api/audit` (solo Administrador, de su organización; filtros `userId`, `entityType`, `entityId`, `action`, `take`).

### Cabeceras

`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, `Permissions-Policy`, `Cache-Control: no-store`, HSTS fuera de desarrollo y sin cabecera `Server`.

### Reglas de roles

Hay 5 roles fijos: `SuperAdmin` (plataforma), `Administrador`, `Técnico`, `Empleado` y `Gerente` (antes `Cliente`). Cada usuario tiene **un solo rol** y pertenece a **una organización** (salvo el SuperAdmin).

- **No hay registro público.** El SuperAdmin crea cada organización con su primer Administrador; el Administrador crea los demás usuarios con contraseña temporal (`POST /api/users`). Hasta cambiarla, la cuenta solo puede usar `auth/me`, `auth/change-password`, `auth/logout` y `organizations/Current`.
- No se puede desactivar ni quitar el rol al **último Administrador activo** de una organización, y un Administrador no puede desactivarse ni quitarse el rol a sí mismo.
- El correo es único en toda la plataforma. El rol `SuperAdmin` no se asigna desde una organización.
- Una organización desactivada no puede iniciar sesión y sus tokens dejan de servir de inmediato.

### Endpoints

Todas las rutas empiezan por `/api`. "Plan Integraciones" significa que la organización debe tener ese plan (si no, `403`).

**Sesión y plataforma**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| POST | `/auth/login` | Anónimo | Inicia sesión (devuelve token, organización, plan y si la contraseña es temporal) |
| POST | `/auth/forgot-password` | Anónimo | Envía un código de recuperación de un solo uso al correo |
| POST | `/auth/reset-password` | Anónimo | Cambia la contraseña con el código recibido |
| GET | `/auth/me` | Autenticado | Usuario de la sesión |
| POST | `/auth/change-password` | Autenticado | Cambia la contraseña propia (obligatorio si es temporal) |
| POST | `/auth/logout` | Autenticado | Cierra todas las sesiones del usuario |
| GET | `/health` | Anónimo | Sonda de salud (la usan `iniciar.sh` / `iniciar.ps1`) |
| GET | `/organizations/Current` | Cualquier rol de empresa | Organización y plan del usuario |
| GET | `/organizations` | SuperAdmin | Lista organizaciones (con número de usuarios y equipos) |
| POST | `/organizations` | SuperAdmin | Crea organización, plan y su primer Administrador |
| PUT | `/organizations/{id}` | SuperAdmin | Cambia nombre, plan o estado (activa/inactiva) |

**Usuarios y auditoría**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| GET | `/roles` | Administrador | Lista los roles |
| GET | `/users` | Administrador | Lista los usuarios de su organización |
| POST | `/users` | Administrador | Crea un usuario con contraseña temporal |
| PUT | `/users/{id}` | Administrador | Edita estado, teléfono y correo |
| PUT | `/users/{id}/role` | Administrador | Asigna el rol |
| PUT | `/users/{id}/password` | Administrador | Restablece la contraseña (queda temporal) |
| GET | `/audit` | Administrador | Eventos de auditoría |

**Equipos, componentes y asignaciones**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| GET | `/equipment/GetEquipmentList` | Adm./Téc./Emp. | Lista equipos (Empleado: solo los asignados) |
| GET | `/equipment/GetEquipmentById/{id}` | Adm./Téc./Emp. | Consulta un equipo (incluye alquiler, fuente externa y campos bloqueados) |
| POST | `/equipment/PostNewEquipment` | Adm./Téc. | Crea un equipo (propio o alquilado) |
| PUT | `/equipment/PutEquipment/{id}` | Adm./Téc. | Corrige los datos (reemplazo completo; código y serie solo Administrador) |
| PATCH | `/equipment/PatchEquipmentStatus/{id}` | Adm./Téc. | Fuera de servicio ⇄ en servicio |
| DELETE | `/equipment/DeleteEquipment/{id}` | Administrador | Da de baja (solo equipos propios) |
| GET | `/equipmentcomponent/GetComponentList` | Adm./Téc. | Lista todos los componentes |
| GET | `/equipmentcomponent/GetComponentById/{id}` | Adm./Téc./Emp. | Consulta un componente (Empleado: de sus equipos) |
| GET | `/equipmentcomponent/GetComponentsByEquipment/{equipmentId}` | Adm./Téc./Emp. | Componentes de un equipo |
| POST | `/equipmentcomponent/PostNewComponent` | Adm./Téc. | Agrega un componente |
| PUT | `/equipmentcomponent/PutComponent/{id}` | Adm./Téc. | Edita un componente |
| DELETE | `/equipmentcomponent/DeleteComponent/{id}` | Adm./Téc. | Borra un componente |
| POST | `/assignment/AssignEquipment` | Adm./Téc. | Asigna un equipo (queda En uso) |
| PATCH | `/assignment/ReleaseEquipment/{id}` | Adm./Téc. | Libera un equipo (vuelve a Disponible) |
| GET | `/assignment/GetAssignments` | Adm./Téc./Emp. | Lista (Empleado: solo las suyas) |

**Incidencias y mantenimiento**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| GET | `/incident/GetIncidentList` | Adm./Téc./Emp. | Lista (Empleado: de sus equipos o reportadas por él) |
| GET | `/incident/GetIncidentById/{id}` | Adm./Téc./Emp. | Consulta una incidencia |
| GET | `/incident/GetIncidentsByEquipment/{equipmentId}` | Adm./Téc./Emp. | Incidencias de un equipo |
| POST | `/incident/PostNewIncident` | Adm./Emp. | Reporta una incidencia (Empleado: solo de sus equipos) |
| PUT | `/incident/PutIncident/{id}` | Adm./Emp. | Edita una incidencia |
| DELETE | `/incident/DeleteIncident/{id}` | Adm./Emp. | Borra una incidencia |
| GET | `/maintenance/GetMaintenanceList` | Cualquier rol de empresa | Lista (Empleado: de sus equipos) |
| GET | `/maintenance/GetMaintenanceById/{id}` | Cualquier rol de empresa | Consulta un mantenimiento |
| GET | `/maintenance/GetMaintenancesByEquipment/{equipmentId}` | Cualquier rol de empresa | Mantenimientos de un equipo |
| GET | `/maintenance/GetHistoryByEquipment/{equipmentId}` | Cualquier rol de empresa | Historial completo (mantenimientos e incidencias) |
| POST | `/maintenance/PostNewMaintenance` | Adm./Téc. | Abre un mantenimiento |
| POST | `/maintenance/CreateFromIncident/{incidentId}` | Adm./Téc. | Abre un correctivo desde una incidencia |
| POST | `/maintenance/CreatePreventiveFromPrediction/{equipmentId}` | Adm./Téc. | Abre un preventivo desde una alerta de riesgo |
| PUT | `/maintenance/PutMaintenance/{id}` | Adm./Téc. | Edita o cancela un mantenimiento abierto |
| POST | `/maintenance/CloseMaintenance/{id}` | Adm./Téc. | Cierra (exige el trabajo realizado) |
| POST | `/maintenance/AddSparePart/{maintenanceId}` | Adm./Téc. | Agrega un repuesto usado |
| DELETE | `/maintenance/RemoveSparePart/{maintenanceId}/{maintenanceSparePartId}` | Adm./Téc. | Quita un repuesto |
| POST | `/maintenance/VoidMaintenance/{id}` | Administrador | Anula un mantenimiento cerrado con errores (con motivo) |
| DELETE | `/maintenance/DeleteMaintenance/{id}` | Adm./Téc. | Borra un mantenimiento no completado |
| GET | `/sparepart/GetSparePartList` | Adm./Téc. | Catálogo de repuestos |
| GET | `/sparepart/GetSparePartById/{id}` | Adm./Téc. | Consulta un repuesto |
| POST | `/sparepart/PostNewSparePart` | Adm./Téc. | Crea un repuesto |
| PUT | `/sparepart/PutSparePart/{id}` | Adm./Téc. | Edita un repuesto |
| PATCH | `/sparepart/DeactivateSparePart/{id}` | Adm./Téc. | Desactiva un repuesto |
| PATCH | `/sparepart/ReactivateSparePart/{id}` | Adm./Téc. | Reactiva un repuesto |

**Cronograma, calendario y notificaciones**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| GET | `/maintenanceplans?equipmentId=` | Adm./Téc./Emp. | Planes preventivos (Empleado: de sus equipos) |
| GET | `/maintenanceplans/Technicians` | Adm./Téc. | Posibles responsables de un plan |
| POST | `/maintenanceplans` | Adm./Téc. | Crea un plan cada N semanas |
| PUT | `/maintenanceplans/{id}` | Adm./Téc. | Edita, pausa o reactiva un plan |
| PATCH | `/maintenanceplans/{id}/Reschedule` | Adm./Téc. | Cambia la próxima fecha (arrastrar en el calendario) |
| POST | `/maintenanceplans/{id}/Start` | Adm./Téc. | Inicia el preventivo programado |
| GET | `/calendar?from=&to=` | Cualquier rol de empresa | Eventos del calendario (máximo 400 días) |
| GET | `/notifications` | Cualquier rol de empresa | Notificaciones propias y cuántas sin leer |
| POST | `/notifications/{id}/Read` | Cualquier rol de empresa | Marca una como leída |
| POST | `/notifications/ReadAll` | Cualquier rol de empresa | Marca todas como leídas |
| POST | `/notifications/RunReminders` | Administrador | Revisa los recordatorios ya (sin esperar la hora) |
| WS | `/hubs/notificaciones` | Autenticado (`?access_token=`) | SignalR: eventos `notificacion` y `datosCambiaron` |

**Alquiler**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| GET | `/rental/Lessors` | Adm./Téc./Ger. | Arrendadores |
| POST | `/rental/Lessors` | Adm./Téc. | Crea un arrendador |
| PUT | `/rental/Lessors/{id}` | Adm./Téc. | Edita un arrendador |
| GET | `/rental/Contracts` | Adm./Téc./Ger. | Contratos con tarifas, equipos y costo mensual |
| GET | `/rental/Contracts/Expiring?days=30` | Adm./Téc./Ger. | Contratos activos por vencer |
| POST | `/rental/Contracts` | Adm./Téc. | Crea un contrato con tarifas por tipo de equipo |
| PUT | `/rental/Contracts/{id}` | Adm./Téc. | Edita un contrato activo |
| POST | `/rental/Contracts/{id}/ReturnAll` | Administrador | Devuelve todos los equipos del contrato |
| POST | `/rental/Equipment/{equipmentId}/Return` | Administrador | Devuelve un equipo alquilado |
| POST | `/rental/Equipment/{equipmentId}/Replacement` | Adm./Téc. | Registra el equipo de reemplazo de uno devuelto |
| GET | `/rental/Claims` | Adm./Téc./Ger. | Reclamos al arrendador |
| POST | `/rental/Claims` | Adm./Téc. | Crea un reclamo (correo al arrendador) |
| PUT | `/rental/Claims/{id}` | Adm./Téc. | Cambia el estado del reclamo y la respuesta |

**IA, glosario y diagnóstico**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| POST | `/assistant/Triage` | Adm./Emp. | Chat "Reportar una falla" (lenguaje cotidiano, máx. 3 preguntas) |
| POST | `/diagnosis/Diagnose` | Adm./Téc./Emp. | Diagnóstico con IA de un síntoma (Empleado: de sus equipos) |
| GET | `/diagnosis/GetDiagnosisById/{id}` | Adm./Téc. | Consulta un diagnóstico |
| GET | `/diagnosis/GetDiagnosesByEquipment/{equipmentId}` | Adm./Téc. | Diagnósticos de un equipo |
| GET | `/diagnosis/GetRecentDiagnoses?take=` | Adm./Téc. | Últimos diagnósticos |
| POST | `/feedback/CreateFeedback` | Adm./Téc. | El técnico confirma o corrige un diagnóstico |
| GET | `/feedback/GetByDiagnosis/{diagnosisId}` | Adm./Téc. | Retroalimentación de un diagnóstico |
| GET | `/feedback/GetRecentFeedbacks` | Adm./Téc. | Últimas retroalimentaciones |
| GET | `/feedback/GetMetrics` | Adm./Téc. | Métricas de acierto de la IA |
| GET | `/errorknowledge/GetKnowledgeList` | Adm./Téc./Emp. | Biblioteca de errores conocidos |
| GET | `/errorknowledge/GetKnowledgeById/{id}` | Adm./Téc./Emp. | Consulta una entrada |
| POST | `/errorknowledge/PostNewKnowledge` | Adm./Téc. | Crea una entrada |
| PUT | `/errorknowledge/PutKnowledge/{id}` | Adm./Téc. | Edita una entrada |
| DELETE | `/errorknowledge/DeleteKnowledge/{id}` | Adm./Téc. | Borra una entrada |
| GET | `/glossary` | Adm./Téc./Emp. | Glosario de modismos (base + propios) |
| POST | `/glossary` | Adm./Téc. | Agrega una expresión propia |
| PUT | `/glossary/{id}` | Adm./Téc. | Edita una expresión propia (las base son de solo lectura) |
| DELETE | `/glossary/{id}` | Adm./Téc. | Borra una expresión propia |
| GET | `/prediction/GetHighRisk` | Adm./Téc. | Equipos con mayor riesgo de falla |
| GET | `/prediction/GetByEquipment/{equipmentId}` | Adm./Téc. | Riesgo de un equipo |
| POST | `/prediction/RefreshAll` | Adm./Téc. | Recalcula el riesgo de todos los equipos |
| POST | `/prediction/RefreshForEquipment/{equipmentId}` | Adm./Téc. | Recalcula el riesgo de un equipo |
| POST | `/prediction/Acknowledge/{predictionId}` | Adm./Téc. | Marca una alerta como atendida |

**Dashboard, reportes, integraciones y mercado**

| Método | Ruta | Autorización | Descripción |
| --- | --- | --- | --- |
| GET | `/dashboard/GetEquipmentSummary` | Adm./Ger. | Equipos por estado |
| GET | `/dashboard/GetOpenIncidents` | Adm./Ger. | Incidencias abiertas |
| GET | `/dashboard/GetUpcomingMaintenances` | Adm./Ger. | Próximos mantenimientos |
| GET | `/dashboard/GetHighRiskEquipments` | Adm./Ger. | Equipos de alto riesgo |
| GET | `/reports/GetEquipments?format=pdf\|excel` | Adm./Ger. | Reporte de equipos (filtros `from`, `to`, `type`, `status`, `assignedTo`) |
| GET | `/reports/GetMaintenances?format=pdf\|excel` | Adm./Ger. | Reporte de mantenimientos |
| GET | `/reports/GetCosts?format=pdf\|excel` | Adm./Ger. | Reporte de costos |
| GET | `/aireports` | Adm./Ger. · plan Integraciones | Reportes con IA |
| GET | `/aireports/{id}` | Adm./Ger. · plan Integraciones | Contenido de un reporte |
| GET | `/aireports/{id}/Pdf` | Adm./Ger. · plan Integraciones | Descarga el PDF |
| POST | `/aireports/Generate` | Adm./Ger. · plan Integraciones | Genera un reporte en segundo plano (por defecto, últimos 30 días) |
| GET | `/market/Equipment/{equipmentId}` | Adm./Téc./Ger. · plan Integraciones | Precio de reemplazo y guías del modelo (con fuentes) |
| POST | `/integrations/Profile` | Administrador · plan Integraciones | Perfila una fuente y sugiere el mapeo (no guarda) |
| GET | `/integrations/Sources` | Administrador · plan Integraciones | Fuentes conectadas y su última sincronización |
| POST | `/integrations/Sources` | Administrador · plan Integraciones | Guarda la fuente e importa los equipos |
| POST | `/integrations/Sources/{id}/Sync` | Administrador · plan Integraciones | Sincroniza ahora |
| DELETE | `/integrations/Sources/{id}` | Administrador · plan Integraciones | Desactiva la fuente (los equipos se conservan) |

En desarrollo, el panel de tareas programadas de Hangfire está en `http://localhost:5255/hangfire` (solo desde el mismo equipo).

### Ejemplos de cuerpo

**Login** — `POST /api/auth/login`

```json
{ "email": "juan@empresa.com", "password": "secreto12345" }
```

Responde `{ "token", "userId", "fullName", "email", "role", "active", "expiresAt", "organizationId", "organizationName", "plan", "mustChangePassword" }` y deja la cookie de sesión.

**Crear usuario** — `POST /api/users` (Administrador)

```json
{
  "fullName": "Juan Pérez",
  "email": "juan@empresa.com",
  "role": "Empleado",
  "temporaryPassword": "Temporal2026x",
  "phone": "3001234567"
}
```

### Reglas de negocio detalladas

#### Equipos

- `internalCode` y `serialNumber` son únicos **dentro de cada organización** (`409`).
- `DELETE /api/equipment/DeleteEquipment/{id}` no borra físicamente: cambia el estado a `DECOMMISSIONED` y conserva todo el historial. Responde `409` si ya está dado de baja, si tiene una asignación `ACTIVE`, si tiene un mantenimiento `OPEN`/`IN_PROGRESS` o si es alquilado (se devuelve, no se da de baja).
- `DECOMMISSIONED` y `RETURNED` son terminales: no vuelven a servicio ni se asignan.
- Un equipo alquilado debe tener un contrato activo con tarifa para su tipo; su fecha de adquisición es el inicio del contrato.
- En un equipo importado de una base de terceros, los campos mapeados no se editan (`409`).

#### Asignaciones

- Un equipo solo puede tener una asignación `ACTIVE` (garantizado también por un índice único, incluso con peticiones simultáneas).
- Solo se asigna un equipo `AVAILABLE` (no en mantenimiento, fuera de servicio, dado de baja ni devuelto), a un usuario activo de personal interno (Empleado, Técnico o Administrador), nunca a un Gerente.

#### Incidentes

- Un equipo solo puede tener un incidente `OPEN` o `IN_PROGRESS` (índice único).
- No se reportan incidentes de equipos dados de baja ni con fecha futura. El Empleado solo reporta sobre los equipos que tiene asignados.
- `IN_PROGRESS` solo lo pone el mantenimiento; con un mantenimiento asociado, el estado lo maneja ese mantenimiento.
- El Empleado solo edita o borra **sus** incidentes mientras están `OPEN` y sin mantenimiento, y solo puede pasarlos a `CLOSED`.
- Un incidente con mantenimiento asociado no se borra.

#### Mantenimiento

- Un equipo solo puede tener un mantenimiento `OPEN` o `IN_PROGRESS` (índice único).
- La incidencia vinculada debe ser del mismo equipo y estar `OPEN`.
- Costos y cantidades no pueden ser negativos; la fecha de inicio no puede ser futura y el próximo mantenimiento no puede ser anterior al inicio.
- **Completar solo con `POST /api/maintenance/CloseMaintenance/{id}`** (exige el trabajo realizado). `PutMaintenance` no acepta `COMPLETED`.
- Al cerrar, el equipo vuelve a servicio o queda `OUT_OF_SERVICE`; no puede quedar `UNDER_MAINTENANCE` ni darse de baja desde ahí. La incidencia queda `RESOLVED` o `CLOSED`. Si es preventivo y el equipo tiene plan, la próxima fecha se calcula sola.
- Cancelar libera el equipo y devuelve la incidencia a `OPEN` (se desvincula y queda anotado en las observaciones).
- Un mantenimiento completado no se borra: si tiene errores, se **anula** (`VOIDED`, con motivo) y se registra uno nuevo.
- Equipo alquilado: el correctivo se reporta al arrendador (reclamo); el preventivo interno solo si el contrato lo permite.

### Enums

Se aceptan solo por nombre (`"OPEN"`); un número (`99`) o un nombre inexistente responde `400`.

| Enum | Valores |
| --- | --- |
| Estado del equipo | `AVAILABLE`, `IN_USE`, `UNDER_MAINTENANCE`, `OUT_OF_SERVICE`, `DECOMMISSIONED`, `RETURNED` |
| Propiedad del equipo | `OWNED`, `RENTED` |
| Estado del mantenimiento | `OPEN`, `IN_PROGRESS`, `COMPLETED`, `CANCELLED`, `VOIDED` |
| Tipo de mantenimiento | `PREVENTIVE`, `CORRECTIVE` |
| Estado de la incidencia | `OPEN`, `IN_PROGRESS`, `RESOLVED`, `CLOSED` |
| Estado de la asignación | `ACTIVE`, `RELEASED` |
| Plan de la organización | `BASICO`, `INTEGRACIONES` |
| Estado del contrato | `ACTIVE`, `ENDED`, `CANCELLED` |
| Estado del reclamo | `OPEN`, `ANSWERED`, `RESOLVED` |

## Documentación de la API

- OpenAPI (modo desarrollo): http://localhost:5255/openapi/v1.json
- Peticiones de ejemplo: [src/Backend.API/Backend.API.http](src/Backend.API/Backend.API.http)
