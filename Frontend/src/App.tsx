import { Routes, Route, Navigate, Outlet } from "react-router-dom";
import HomePage from "./pages/HomePage";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import { EquipoListPage } from "./pages/EquipoListPage";
import { EquipoFormPage } from "./pages/EquipoFormPage";
import UsersPage from "./pages/UsersPage";
import ProtectedRoute from "./components/ProtectedRoute";
import Navbar from "./components/Navbar";

function Layout() {
  return (
    <>
      <Navbar />
      <Outlet />
    </>
  );
}

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<h1 className="p-8">Dashboard (pendiente)</h1>} />
          <Route path="/equipos" element={<EquipoListPage />} />
          <Route path="/equipos/nuevo" element={<EquipoFormPage />} />
          <Route path="/equipos/:id/editar" element={<EquipoFormPage />} />
        </Route>

        <Route element={<ProtectedRoute allowedRoles={["Administrador"]} />}>
          <Route path="/usuarios" element={<UsersPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" />} />
      </Route>
    </Routes>
  );
}