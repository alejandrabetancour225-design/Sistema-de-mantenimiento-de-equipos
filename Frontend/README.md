# Sistema de Gestión de Mantenimiento de Equipos

Plataforma para centralizar el inventario de equipos tecnológicos de una empresa,
sus componentes, incidentes, mantenimientos, repuestos y usuarios. Proyecto
académico desarrollado con metodología Scrum.

## Estado del proyecto

### Implementado y funcionando de punta a punta

- **Autenticación**: registro y login con JWT. El primer usuario registrado en
  todo el sistema se vuelve **Administrador** automáticamente; los siguientes
  quedan como **Cliente** por defecto. Un Administrador reasigna roles después.
- **Equipos**: registrar, consultar, editar, cambiar estado y dar de baja.
- **Usuarios**: listado, activar/desactivar y reasignar rol (solo Administrador).
- **Asignaciones**: asignar/liberar un equipo a un empleado (Admin/Técnico);
  cada usuario consulta sus propias asignaciones (Admin/Técnico ven todas).
- **Componentes de equipo**: piezas internas de un equipo (RAM, disco, etc.),
  CRUD disponible desde el detalle del equipo (Admin/Técnico).
- **Incidentes**: reportar fallas sobre un equipo y seguir su estado
  (Admin/**Empleado** — nota que aquí Técnico no participa, a diferencia del
  resto de módulos).
- **Mantenimientos** (preventivo/correctivo): se registran desde el detalle de
  cada equipo (Admin/Técnico). Incluyen técnico responsable, problema
  reportado, costos de mano de obra y otros costos, repuestos usados y cierre
  con "trabajo realizado". Ver [Módulo de mantenimiento](#módulo-de-mantenimiento).
- **Repuestos**: catálogo con costo unitario, activar/desactivar y edición de
  costo, en la página `/repuestos` (Admin/Técnico).

### Pendiente

- **Dashboard** y **Reportes**: no implementados todavía (`/dashboard` muestra
  un placeholder).
- **Auditoría**: no implementado.
- **Mantenimiento — lo que el backend ya soporta y el front aún no expone**:
  - Crear un mantenimiento **desde un incidente** (`CreateFromIncident`) y
    vincular un `incidentId` al crearlo.
  - **Editar** un mantenimiento (`PutMaintenance`) y cambiar su estado a
    `IN_PROGRESS` o `CANCELLED`.
  - Elegir el **estado final del equipo y del incidente** al cerrar
    (`equipmentFinalStatus` / `incidentFinalStatus`). Hoy el front no los envía,
    así que el backend usa los valores por defecto: equipo → `AVAILABLE`,
    incidente → `RESOLVED`.
  - Capturar `nextMaintenanceDate`, `observations`, y costos al cerrar.
  - Ver el **historial y costo total acumulado** por equipo (`GetHistoryByEquipment`)
    y un listado global (`GetMaintenanceList`). Los servicios ya existen en
    `maintenanceServices.ts`, falta la UI.
- **Técnico no puede elegirse en el formulario si no es Admin**: la lista de
  técnicos sale de `/users` (solo Administrador). Un Técnico se asigna a sí
  mismo automáticamente (`userId` del contexto de auth).
- **HU-06 (transiciones de estado maestras)**: el backend permite cambiar el
  estado de un equipo a cualquier valor sin validar una secuencia lógica
  (ej. de "Dado de baja" podría volver a "Disponible"). Falta esa regla.

## Stack técnico

| Capa | Tecnología |
| --- | --- |
| Backend | ASP.NET Core (.NET 10) + C# |
| Base de datos | PostgreSQL (local vía Docker, o compartida en Neon) + Entity Framework Core |
| Frontend | React 19 + TypeScript + Vite |
| Enrutamiento | React Router (`HashRouter`, pensado para empaquetar como app de escritorio) |
| Datos remotos | TanStack Query (React Query) + Axios |
| Formularios | React Hook Form + Zod (login/registro); `useState` en los formularios de mantenimiento y repuestos |
| Estilos | Tailwind CSS 4 |

## Roles del sistema

`Administrador` · `Técnico` · `Empleado` · `Cliente`

Permisos por módulo:

| Módulo | Administrador | Técnico | Empleado | Cliente |
| --- | --- | --- | --- | --- |
| Equipos (ver) | ✅ | ✅ | ✅ | ✅ |
| Equipos (crear/editar/baja) | ✅ | ✅ | ❌ | ❌ |
| Componentes de equipo | ✅ | ✅ | ❌ | ❌ |
| Incidentes | ✅ | ❌ | ✅ | ❌ |
| Mantenimientos (ver y gestionar) | ✅ | ✅ | ❌ | ❌ |
| Repuestos (ver y gestionar) | ✅ | ✅ | ❌ | ❌ |
| Asignaciones (ver todas / asignar) | ✅ | ✅ | solo las propias | solo las propias |
| Usuarios | ✅ | ❌ | ❌ | ❌ |

> En el front, la sección de mantenimientos se muestra en el detalle del equipo
> solo a Admin/Técnico, y el enlace "Repuestos" del sidebar también.

## Estados

- **Equipo** (`EquipmentStatus`): `AVAILABLE` (Disponible) · `IN_USE` (En uso) ·
  `UNDER_MAINTENANCE` (En mantenimiento) · `OUT_OF_SERVICE` (Fuera de servicio) ·
  `DECOMMISSIONED` (Dado de baja)
- **Asignación** (`AssignmentStatus`): `ACTIVE` · `RELEASED`
- **Incidente** (`IncidentStatus`): `OPEN` · `IN_PROGRESS` · `CLOSED` · `RESOLVED`
- **Mantenimiento** (`MaintenanceStatus`): `OPEN` (Abierto) · `IN_PROGRESS`
  (En progreso) · `COMPLETED` (Completado) · `CANCELLED` (Cancelado)
- **Tipo de mantenimiento** (`MaintenanceType`): `PREVENTIVE` (Preventivo) ·
  `CORRECTIVE` (Correctivo)

## Módulo de mantenimiento

### Flujo en la interfaz

1. Entrar a **Equipos → detalle de un equipo**; al final aparece la sección
   **Mantenimientos** (solo Admin/Técnico).
2. Llenar el formulario: técnico (el Admin lo elige; el Técnico queda asignado
   a sí mismo), tipo, problema reportado, costo de mano de obra y otros costos →
   **Registrar mantenimiento**.
3. Cada mantenimiento de la lista tiene un botón **Detalle** que permite:
   - **Agregar repuestos** (solo repuestos activos, con cantidad) y **quitarlos**.
   - **Cerrar mantenimiento** escribiendo el "trabajo realizado".
   - **Eliminar mantenimiento**.
4. Cuando el mantenimiento está `COMPLETED` o `CANCELLED` queda de solo lectura
   (no se pueden agregar/quitar repuestos, cerrar ni eliminar desde la UI).

### Reglas de negocio (aplicadas por el backend)

- Al **crear** un mantenimiento, el equipo pasa a `UNDER_MAINTENANCE`. Si se
  vincula a un incidente, este pasa a `IN_PROGRESS`.
- Un incidente solo puede tener **un** mantenimiento vinculado.
- El técnico debe estar **activo** y ser `Técnico` o `Administrador`.
- Un mantenimiento `COMPLETED`/`CANCELLED` no admite más cambios.
- Al **cerrar**, el mantenimiento pasa a `COMPLETED`; el equipo vuelve a
  `AVAILABLE` y el incidente vinculado a `RESOLVED` (salvo que se indique otro
  estado final en la petición).
- Un repuesto **inactivo** no se puede agregar, y el mismo repuesto no puede
  repetirse dentro de un mismo mantenimiento.
- `unitCostAtUse` guarda el costo del repuesto **al momento de usarlo**, así que
  cambiar el costo en el catálogo no altera mantenimientos anteriores.
- `totalCost = laborCost + otherCosts + sparePartsCost`.

### Endpoints que consume el front

Base: `VITE_API_URL` (ej. `http://localhost:5255/api`).

| Método | Ruta | Uso | Usado en UI |
| --- | --- | --- | --- |
| GET | `/maintenance/GetMaintenancesByEquipment/{equipmentId}` | Lista por equipo | ✅ |
| POST | `/maintenance/PostNewMaintenance` | Crear | ✅ |
| POST | `/maintenance/CloseMaintenance/{id}` | Cerrar | ✅ |
| POST | `/maintenance/AddSparePart/{maintenanceId}` | Agregar repuesto | ✅ |
| DELETE | `/maintenance/RemoveSparePart/{maintenanceId}/{maintenanceSparePartId}` | Quitar repuesto | ✅ |
| DELETE | `/maintenance/DeleteMaintenance/{id}` | Eliminar | ✅ |
| PUT | `/maintenance/PutMaintenance/{id}` | Editar / cambiar estado | ⏳ servicio listo |
| POST | `/maintenance/CreateFromIncident/{incidentId}` | Crear desde incidente | ⏳ servicio listo |
| GET | `/maintenance/GetHistoryByEquipment/{equipmentId}` | Historial y costos | ⏳ servicio listo |
| GET | `/sparepart/GetSparePartList?includeInactive=` | Listar repuestos | ✅ |
| POST | `/sparepart/PostNewSparePart` | Crear repuesto | ✅ |
| PUT | `/sparepart/PutSparePart/{id}` | Editar (costo, nombre...) | ✅ |
| PATCH | `/sparepart/DeactivateSparePart/{id}` · `/ReactivateSparePart/{id}` | Activar/desactivar | ✅ |

Todos requieren JWT. Las escrituras exigen rol Administrador o Técnico.

### Archivos del front relacionados

```md
Frontend/src/
├── components/MaintenanceSection.tsx   # Formulario + lista + detalle (se monta en EquipoDetailPage)
├── hooks/useMaintenance.ts             # useMaintenancesByEquipment, useSpareParts
├── pages/SparePartsPage.tsx            # Catálogo de repuestos (/repuestos)
├── services/maintenanceServices.ts     # Llamadas HTTP de mantenimiento
├── services/sparePartServices.ts       # Llamadas HTTP de repuestos
├── types/maintenance.ts                # Tipos, etiquetas en español y DTOs
└── types/sparePart.ts
```

## Rutas del frontend

| Ruta | Acceso | Página |
| --- | --- | --- |
| `/` | Público | Login |
| `/register` | Público | Registro |
| `/dashboard` | Autenticado | Placeholder (pendiente) |
| `/equipos`, `/equipos/nuevo`, `/equipos/:id`, `/equipos/:id/editar` | Autenticado | Equipos (el detalle incluye componentes, incidentes y mantenimientos) |
| `/asignaciones` | Autenticado | Asignaciones |
| `/repuestos` | Administrador, Técnico | Catálogo de repuestos |
| `/usuarios` | Administrador | Gestión de usuarios |

## Estructura del repositorio

```md
Backend/
├── src/Backend.API/        # API .NET (Controllers, Services, Models, DTOs, Data, Migrations)
├── Frontend/                # Aplicación React + Vite
│   └── src/
│       ├── services/          # Llamadas HTTP a la API (api.ts, authServices.ts, equipmentServices.ts, maintenanceServices.ts...)
│       ├── components/        # Sidebar, ProtectedRoute, LoginCard, StatusBadge, MaintenanceSection...
│       ├── context/           # authContext (AuthProvider / useAuth)
│       ├── hooks/              # useAuthMutations, useEquipmentList, useUsers, useAssignments, useMaintenance...
│       ├── pages/               # LoginPage, RegisterPage, EquipoListPage, EquipoDetailPage, SparePartsPage, UsersPage...
│       ├── schemas/             # Validaciones con Zod
│       └── types/                # Tipos TypeScript calcados de los DTOs del backend
├── docker-compose.yml       # Postgres local (opcional, alternativa a Neon)
└── .env (no se sube)        # Secretos locales
```

## Requisitos previos

- .NET SDK 10
- Node.js 18+ y npm
- Docker Desktop (si usas Postgres local) **o** acceso a la base compartida en Neon

## Cómo correrlo

### Opción A: base de datos compartida en Neon (recomendada para trabajar en equipo)

```powershell
dotnet user-secrets init --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Password" "<password-de-neon>" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Jwt:Key" "<el-mismo-que-usa-el-equipo>" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Encryption:Key" "<la-misma-que-usa-el-equipo>" --project src/Backend.API/Backend.API.csproj
```

`Jwt:Key` y `Encryption:Key` **deben ser idénticos** a los del resto del equipo
(si no, no vas a poder leer correos/teléfonos cifrados ni validar tokens).
`Database:Password` sí es exclusivo de la base de Neon.

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Backend.API --startup-project src/Backend.API
dotnet run --project src/Backend.API
```

> La API también aplica las migraciones automáticamente al iniciar. La última
> migración, `AddMaintenanceAndSpareParts`, crea las tablas de mantenimientos y
> repuestos: **haz `git pull` y reinicia la API** antes de probar el módulo.

### Opción B: Postgres local con Docker (independiente, no requiere nada del equipo)

```powershell
docker compose up -d
dotnet user-secrets init --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Host" "localhost" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Name" "database" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:User" "postgres" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Database:Password" "postgres" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Jwt:Key" "<genera-tu-propia-clave-larga>" --project src/Backend.API/Backend.API.csproj
dotnet user-secrets set "Encryption:Key" "<genera-tu-propia-clave-base64-de-32-bytes>" --project src/Backend.API/Backend.API.csproj
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/Backend.API --startup-project src/Backend.API
dotnet run --project src/Backend.API
```

Backend disponible en `http://localhost:5255`.

### Frontend

```powershell
cd Frontend
npm install
npm run dev
```

Vite abre en `http://localhost:5173`. Confirma que `Frontend/.env` (créalo si no
existe, no se sube al repo) tenga:

```env
VITE_API_URL=http://localhost:5255/api
```

Otros scripts: `npm run build` (compila TypeScript y genera el build),
`npm run lint` y `npm run preview`.

### Probar el módulo de mantenimiento

1. Inicia sesión como **Administrador** o **Técnico** (con Empleado/Cliente no
   verás la sección ni el enlace de Repuestos).
2. Crea al menos un repuesto en **Repuestos** (`/repuestos`).
3. Abre el detalle de un equipo, registra un mantenimiento y agrégale el repuesto.
4. Ciérralo y verifica que el equipo vuelve a "Disponible".

## Notas importantes

- `Encryption:Key` **nunca debe cambiarse** una vez que hay datos guardados —
  cifra correos y teléfonos; cambiarla hace ilegibles los datos existentes.
- Si usas Neon, el **primer usuario registrado en esa base compartida** es el
  que queda como Administrador — probablemente ya exista uno, pídele a tu
  equipo el usuario de prueba en vez de registrarte esperando ser admin.
- El `.env` del frontend y los `user-secrets` del backend son locales a cada
  máquina — no se suben a git. Cada quien los configura una vez.

## Equipo del proyecto

| Rol | Responsabilidad |
| --- | --- |
| Cliente | Necesidad del negocio, prioridades y validación |
| Product Owner | Gestionar y priorizar el Product Backlog |
| Scrum Master | Facilitar Scrum y eliminar impedimentos |
| Frontend | Interfaz y experiencia de usuario |
| Backend | Lógica, servicios, datos e integración |
| QA | Pruebas y validación de calidad |
| Seguridad | Requisitos, riesgos y controles de seguridad |
