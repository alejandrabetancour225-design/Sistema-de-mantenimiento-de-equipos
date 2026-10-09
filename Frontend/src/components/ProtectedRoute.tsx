import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/authContext";
import { roleHome } from "../utils/roleHome";
import type { Role } from "../types/auth";

interface Props {
  allowedRoles?: Role[];
}

// La restricción real vive en el backend ([Authorize(Roles=...)]); esto es
// solo UX para no mostrar pantallas que el usuario no puede abrir.
export default function ProtectedRoute({ allowedRoles }: Props) {
  const { isAuthenticated, role } = useAuth();

  if (!isAuthenticated) return <Navigate to="/" replace />;

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    return <Navigate to={roleHome(role)} replace />;
  }

  return <Outlet />;
}