import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/authContext";
import type { Role } from "../types/auth";

interface Props {
  allowedRoles?: Role[];
}

// La restricción real vive en el backend ([Authorize(Roles=...)]); esto es
// solo UX para ocultar pantallas que el usuario no debería ver.
export default function ProtectedRoute({ allowedRoles }: Props) {
  const { isAuthenticated, role } = useAuth();

  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}