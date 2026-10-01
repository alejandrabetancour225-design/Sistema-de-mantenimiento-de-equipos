# Sistema de Gestión de Mantenimiento de Equipos

Plataforma para centralizar el inventario de equipos tecnológicos de una empresa,
sus componentes, incidentes, mantenimientos y usuarios. Proyecto académico
desarrollado con metodología Scrum.

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

### Pendiente

- **Dashboard** y **Reportes**: no implementados en el backend todavía.
- **Auditoría**: no implementado.
- **Mantenimiento** (preventivo/correctivo) como entidad propia: existe la
  referencia (`maintenanceId`) en Incidentes, pero el módulo de Mantenimiento
  en sí (registrar técnico, repuestos, costos) aún no tiene endpoints.
- **HU-06 (transiciones de estado maestras)**: el backend permite cambiar el
  estado de un equipo a cualquier valor sin validar una secuencia lógica
  (ej. de "Dado de baja" podría volver a "Disponible"). Falta esa regla.

## Stack técnico

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core (.NET 10) + C# |
| Base de datos | PostgreSQL (local vía Docker, o compartida en Neon) + Entity Framework Core |
| Frontend | React + TypeScript + Vite |
| Enrutamiento | React Router (`HashRouter`, pensado para empaquetar como app de escritorio) |
| Datos remotos | TanStack Query (React Query) |
| Formularios | React Hook Form + Zod |
| Estilos | Tailwind CSS |

## Roles del sistema

`Administrador` · `Técnico` · `Empleado` · `Cliente`

Permisos por módulo:

| Módulo | Administrador | Técnico | Empleado | Cliente |
|---|---|---|---|---|
| Equipos (ver) | ✅ | ✅ | ✅ | ✅ |
| Equipos (crear/editar/baja) | ✅ | ✅ | ❌ | ❌ |
| Componentes de equipo | ✅ | ✅ | ❌ | ❌ |
| Incidentes | ✅ | ❌ | ✅ | ❌ |
| Asignaciones (ver todas / asignar) | ✅ | ✅ | solo las propias | solo las propias |
| Usuarios | ✅ | ❌ | ❌ | ❌ |

## Estados

- **Equipo** (`EquipmentStatus`): `AVAILABLE` (Disponible) · `IN_USE` (En uso) ·
  `UNDER_MAINTENANCE` (En mantenimiento) · `OUT_OF_SERVICE` (Fuera de servicio) ·
  `DECOMMISSIONED` (Dado de baja)
- **Asignación** (`AssignmentStatus`): `ACTIVE` · `RELEASED`
- **Incidente** (`IncidentStatus`): `OPEN` · `IN_PROGRESS` · `CLOSED` · `RESOLVED`

## Estructura del repositorio

```
Backend/
├── src/Backend.API/        # API .NET (Controllers, Services, Models, DTOs, Data, Migrations)
├── Frontend/                # Aplicación React + Vite
│   └── src/
│       ├── services/          # Llamadas HTTP a la API (api.ts, authServices.ts, equipmentServices.ts...)
│       ├── components/        # Sidebar, ProtectedRoute, LoginCard, StatusBadge...
│       ├── context/           # authContext (AuthProvider / useAuth)
│       ├── hooks/              # useAuthMutations, useEquipmentList, useUsers, useAssignments...
│       ├── pages/               # LoginPage, RegisterPage, EquipoListPage, EquipoDetailPage, UsersPage...
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

```
VITE_API_URL=http://localhost:5255/api
```

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
|---|---|
| Cliente | Necesidad del negocio, prioridades y validación |
| Product Owner | Gestionar y priorizar el Product Backlog |
| Scrum Master | Facilitar Scrum y eliminar impedimentos |
| Frontend | Interfaz y experiencia de usuario |
| Backend | Lógica, servicios, datos e integración |
| QA | Pruebas y validación de calidad |
| Seguridad | Requisitos, riesgos y controles de seguridad |