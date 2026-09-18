# Sistema de Gestión de Mantenimiento de Equipos

Plataforma para centralizar el inventario de equipos tecnológicos de una empresa,
sus mantenimientos, incidentes y usuarios. Proyecto académico desarrollado con
metodología Scrum.

## Estado del proyecto (Sprint 1)

Implementado y funcionando de punta a punta:

- **Autenticación**: registro y login con JWT. El primer usuario registrado en
  el sistema se convierte en Administrador automáticamente; los siguientes
  quedan como Cliente por defecto.
- **Equipos**: registrar, consultar, editar, cambiar estado y dar de baja.
- **Usuarios**: listado, activar/desactivar y reasignar rol (solo Administrador).

Pendiente (no implementado aún en el backend): Asignaciones, Mantenimiento,
Dashboard, Reportes, Auditoría e Inteligencia Artificial. Existen en el diagrama
de clases conceptual pero no como código.

## Stack técnico

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core (.NET 10) + C# |
| Base de datos | PostgreSQL + Entity Framework Core |
| Frontend | React + TypeScript + Vite |
| Enrutamiento | React Router (`HashRouter`, pensado para app de escritorio) |
| Datos remotos | TanStack Query (React Query) |
| Formularios | React Hook Form + Zod |
| Estilos | Tailwind CSS |

## Roles del sistema

`Administrador` · `Técnico` · `Empleado` · `Cliente`

## Estados de equipo

`AVAILABLE` (Disponible) · `IN_USE` (En uso) · `UNDER_MAINTENANCE` (En mantenimiento)
· `OUT_OF_SERVICE` (Fuera de servicio) · `DECOMMISSIONED` (Dado de baja)

## Estructura del repositorio

```
Backend/
├── src/Backend.API/       # API .NET (Controllers, Services, Models, DTOs, Data)
├── Frontend/               # Aplicación React + Vite
│   └── src/
│       ├── services/         # Llamadas HTTP a la API
│       ├── components/       # Componentes reutilizables (Navbar, ProtectedRoute...)
│       ├── context/          # AuthContext
│       ├── hooks/            # Hooks de datos (useEquipmentList, useUsers...)
│       ├── pages/             # Pantallas (Login, Equipos, Usuarios...)
│       ├── schemas/           # Validaciones con Zod
│       └── types/              # Tipos TypeScript (calcados de los DTOs del backend)
```

## Requisitos previos

- .NET SDK 10
- Node.js 18+ y npm
- PostgreSQL corriendo localmente (o accesible)

## Cómo correrlo

### 1. Backend

```powershell
cd src/Backend.API
dotnet user-secrets set "Jwt:Key" "<clave-de-al-menos-32-caracteres>"
dotnet user-secrets set "Encryption:Key" "<clave-base64-de-32-bytes>"
dotnet run
```

Por defecto queda escuchando en `http://localhost:5255`.

### 2. Frontend

```powershell
cd Frontend
npm install
npm run dev
```

Vite abre en `http://localhost:5173`. Asegúrate de que `Frontend/.env` tenga:

```
VITE_API_URL=http://localhost:5255/api
```

Y que el backend permita ese origen en `appsettings.json`:

```json
"Cors": { "AllowedOrigins": ["http://localhost:5173"] }
```

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