import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/authContext";
import { roleHome } from "../utils/roleHome";
import type { Role } from "../types/auth";

interface Props {
  allowedRoles?: Role[];
}

// La restricción real vive en el backend ([Authorize(Roles=...)]); esto es
// solo UX para no mostrar pantallas que el usuario no puede abrir.
export default function ProtectedRoute({ allowedRoles }: Props) {
  const { isAuthenticated, role, mustChangePassword } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) return <Navigate to="/" replace />;

  // Contraseña temporal (creada por un Administrador): hay que cambiarla antes de seguir.
  if (mustChangePassword && location.pathname !== "/cambiar-contrasena") {
    return <Navigate to="/cambiar-contrasena" replace />;
  }

  if (allowedRoles && (!role || !allowedRoles.includes(role))) {
    // roleHome (/dashboard) está abierto a todos los roles, así que esto nunca entra en bucle.
    return <Navigate to={roleHome(role)} replace />;
  }

  return <Outlet />;
}