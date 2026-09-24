import { Routes, Route, Navigate, Outlet } from "react-router-dom";

import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import { EquipoListPage } from "./pages/EquipoListPage";
import { EquipoFormPage } from "./pages/EquipoFormPage";
import UsersPage from "./pages/UsersPage";
import AssignmentsPage from "./pages/AssignmentsPage";
import ProtectedRoute from "./components/ProtectedRoute";
import Sidebar from "./components/Sidebar";

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

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<h1 className="p-8">Dashboard (pendiente)</h1>} />
          <Route path="/equipos" element={<EquipoListPage />} />
          <Route path="/equipos/nuevo" element={<EquipoFormPage />} />
          <Route path="/equipos/:id/editar" element={<EquipoFormPage />} />
          <Route path="/asignaciones" element={<AssignmentsPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador"]} />}>
          <Route path="/usuarios" element={<UsersPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" />} />
      </Route>
    </Routes>
  );
}