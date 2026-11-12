import { Routes, Route, Navigate, Outlet } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import OrganizationsPage from "./pages/OrganizationsPage";
import ChangePasswordPage from "./pages/ChangePasswordPage";
import { EquipoListPage } from "./pages/EquipoListPage";
import { EquipoFormPage } from "./pages/EquipoFormPage";
import EquipoDetailPage from "./pages/EquipoDetailPage";
import UsersPage from "./pages/UsersPage";
import AssignmentsPage from "./pages/AssignmentsPage";
import SparePartsPage from "./pages/SparePartsPage";
import MaintenanceHistoryPage from "./pages/MaintenanceHistoryPage";
import ProtectedRoute from "./components/ProtectedRoute";
import Sidebar from "./components/Sidebar";
import DashboardPage from "./pages/DashboardPage";
import StaffDashboardPage from "./pages/StaffDashboardPage";
import ReportsPage from "./pages/ReportsPage";
import EquipmentHistoryPage from "./pages/EquipmentHistoryPage";
import AuditPage from "./pages/AuditPage";
import ErrorKnowledgePage from "./pages/ErrorKnowledgePage";
import { useAuth } from "./context/authContext";
import { roleHome } from "./utils/roleHome";
import DiagnosisPage from "./pages/DiagnosisPage";
import AIMetricsPage from "./pages/AIMetricsPage";
import AssistantPage from "./pages/AssistantPage";
import GlossaryPage from "./pages/GlossaryPage";
import RentalPage from "./pages/RentalPage";
import CalendarPage from "./pages/CalendarPage";
import LandingPage from "./pages/LandingPage";
function Layout() {
  return (
    <div className="flex min-h-screen">
      <Sidebar />
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  );
}

// "/" muestra el login; si ya hay sesión, manda a la pantalla del rol.
function Landing() {
  const { isAuthenticated, role } = useAuth();
  return isAuthenticated ? <Navigate to={roleHome(role)} replace /> : <LandingPage />;
}

// Cada rol ve su propio dashboard: Administrador y Gerente el gerencial (endpoints /dashboard),
// Técnico y Empleado uno armado con los datos que sí pueden consultar.
function DashboardRoute() {
  const { role } = useAuth();
  return role === "Administrador" || role === "Gerente" ? <DashboardPage /> : <StaffDashboardPage />;
}

function LoginRoute() {
  const { isAuthenticated, role } = useAuth();
  return isAuthenticated ? <Navigate to={roleHome(role)} replace /> : <LoginPage />;
}
export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Landing />} />
         <Route path="/login" element={<LoginRoute />} />

        {/* Cualquier usuario autenticado, incluido el Gerente */}
        <Route element={<ProtectedRoute />}>
          <Route path="/cambiar-contrasena" element={<ChangePasswordPage />} />
        </Route>

        {/* Plataforma: organizaciones y planes */}
        <Route element={<ProtectedRoute allowedRoles={["SuperAdmin"]} />}>
          <Route path="/organizaciones" element={<OrganizationsPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico", "Empleado", "Gerente"]} />}>
          <Route path="/dashboard" element={<DashboardRoute />} />
          <Route path="/calendario" element={<CalendarPage />} />
          <Route path="/mantenimientos" element={<MaintenanceHistoryPage />} />
          <Route path="/equipos/:id/historial" element={<EquipmentHistoryPage />} />
        </Route>

        {/* El backend solo expone reportes a Administrador y Gerente */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Gerente"]} />}>
          <Route path="/reportes" element={<ReportsPage />} />
        </Route>

        {/* Equipos alquilados */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico", "Gerente"]} />}>
          <Route path="/alquiler" element={<RentalPage />} />
        </Route>

        {/* Personal interno: Administrador, Técnico y Empleado (no Gerente) */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico", "Empleado"]} />}>
          <Route path="/equipos" element={<EquipoListPage />} />
          <Route path="/equipos/:id" element={<EquipoDetailPage />} />
          <Route path="/asignaciones" element={<AssignmentsPage />} />
          <Route path="/glosario" element={<GlossaryPage />} />
        </Route>

        {/* Reportar una falla en lenguaje cotidiano (asistente IA) */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Empleado"]} />}>
          <Route path="/asistente" element={<AssistantPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico"]} />}>
          <Route path="/equipos/nuevo" element={<EquipoFormPage />} />
          <Route path="/equipos/:id/editar" element={<EquipoFormPage />} />
          <Route path="/repuestos" element={<SparePartsPage />} />
          <Route path="/diagnosis" element={<DiagnosisPage />} />
          <Route path="/error-knowledge" element={<ErrorKnowledgePage />} />
          <Route path="/ai-metrics" element={<AIMetricsPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador"]} />}>
          <Route path="/usuarios" element={<UsersPage />} />
          <Route path="/auditoria" element={<AuditPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
