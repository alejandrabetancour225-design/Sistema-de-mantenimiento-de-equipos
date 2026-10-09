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
import { useAuth } from "./context/authContext";
import { roleHome } from "./utils/roleHome";

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
          <Route path="/dashboard" element={<h1 className="p-8">Dashboard (pendiente)</h1>} />
          <Route path="/mantenimientos" element={<MaintenanceHistoryPage />} />
        </Route>

        {/* Personal interno: Administrador, Técnico y Empleado (no Cliente) */}
        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico", "Empleado"]} />}>
          <Route path="/equipos" element={<EquipoListPage />} />
          <Route path="/equipos/:id" element={<EquipoDetailPage />} />
          <Route path="/asignaciones" element={<AssignmentsPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador", "Técnico"]} />}>
          <Route path="/equipos/nuevo" element={<EquipoFormPage />} />
          <Route path="/equipos/:id/editar" element={<EquipoFormPage />} />
          <Route path="/repuestos" element={<SparePartsPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador"]} />}>
          <Route path="/usuarios" element={<UsersPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}