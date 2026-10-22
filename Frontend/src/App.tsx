import { Routes, Route, Navigate, Outlet } from "react-router-dom";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
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
import ReportsPage from "./pages/ReportsPage";
import AuditPage from "./pages/AuditPage";
import ErrorKnowledgePage from "./pages/ErrorKnowledgePage";
import { useAuth } from "./context/authContext";
import { roleHome } from "./utils/roleHome";
import DiagnosisPage from "./pages/DiagnosisPage";
import AIMetricsPage from "./pages/AIMetricsPage";

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
  return isAuthenticated ? <Navigate to={roleHome(role)} replace /> : <LoginPage />;
}

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Landing />} />
        <Route path="/register" element={<RegisterPage />} />

        {/* Cualquier usuario autenticado, incluido el Cliente */}
        <Route element={<ProtectedRoute />}>
          <Route path="/mantenimientos" element={<MaintenanceHistoryPage />} />
        </Route>

        {/* El backend solo expone dashboard y reportes a Administrador y Cliente */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Cliente"]} />}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/reportes" element={<ReportsPage />} />
        </Route>

        {/* Personal interno: Administrador, Técnico y Empleado (no Cliente) */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico", "Empleado"]} />}>
          <Route path="/equipos" element={<EquipoListPage />} />
          <Route path="/equipos/:id" element={<EquipoDetailPage />} />
          <Route path="/asignaciones" element={<AssignmentsPage />} />
          <Route path="/diagnosis" element={<DiagnosisPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico"]} />}>
          <Route path="/equipos/nuevo" element={<EquipoFormPage />} />
          <Route path="/equipos/:id/editar" element={<EquipoFormPage />} />
          <Route path="/repuestos" element={<SparePartsPage />} />
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
