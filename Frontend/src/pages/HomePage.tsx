import { Link } from "react-router-dom";
import { useAuth } from "../context/authContext";
import RoleBadge from "../components/RoleBadge";
import type { Role } from "../types/auth";

interface QuickLink {
  to: string;
  label: string;
}

// Accesos directos por rol. Es solo UX: el backend sigue siendo quien
// realmente restringe cada endpoint (ver README de permisos por rol).
const QUICK_LINKS: Record<Role, QuickLink[]> = {
  Administrador: [
    { to: "/equipos", label: "Ver equipos" },
    { to: "/equipos/nuevo", label: "Registrar equipo" },
    { to: "/usuarios", label: "Gestionar usuarios" },
  ],
  "Técnico": [
    { to: "/equipos", label: "Ver equipos" },
    { to: "/equipos/nuevo", label: "Registrar equipo" },
  ],
  Empleado: [
    { to: "/equipos", label: "Consultar equipos" },
  ],
  Cliente: [
    { to: "/equipos", label: "Consultar equipos" },
  ],
};

const WELCOME_MESSAGE: Record<Role, string> = {
  Administrador: "Tienes control total sobre usuarios y equipos.",
  "Técnico": "Desde aquí puedes gestionar el inventario y el mantenimiento de equipos.",
  Empleado: "Consulta el estado de los equipos asignados a tu área.",
  Cliente: "Consulta el estado de tus equipos.",
};

export default function HomePage() {
  const { isAuthenticated, fullName, role } = useAuth();

  if (!isAuthenticated || !role) {
    return (
      <div className="min-h-[calc(100vh-72px)] flex flex-col items-center justify-center bg-gray-50 text-center px-4">
        <div className="flex gap-4">
          <Link
            to="/register"
            className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
          >
            Registrarse
          </Link>
          <Link
            to="/login"
            className="bg-white text-blue-600 border border-blue-600 px-6 py-2 rounded hover:bg-blue-50"
          >
            Iniciar sesión
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-[calc(100vh-72px)] flex flex-col items-center justify-center bg-gray-50 text-center px-4">
      <div className="space-y-4 max-w-md w-full">
        <div className="flex items-center justify-center gap-2">
          <h1 className="text-2xl font-bold text-gray-700">¡Hola {fullName}!</h1>
          <RoleBadge role={role} />
        </div>

        <p className="text-gray-500">{WELCOME_MESSAGE[role]}</p>

        <div className="flex flex-col gap-3 items-center pt-2">
          {QUICK_LINKS[role].map((link) => (
            <Link
              key={link.to}
              to={link.to}
              className="w-full sm:w-64 bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
            >
              {link.label}
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}