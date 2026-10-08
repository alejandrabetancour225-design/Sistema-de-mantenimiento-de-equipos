import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/authContext";

export default function Sidebar() {
  const { isAuthenticated, role, fullName, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  if (!isAuthenticated) return null;

  function handleLogout() {
    logout();
    navigate("/login");
  }

  const isActive = (path: string) => location.pathname === path;
  const linkClass = (path: string) =>
    `block px-4 py-2 rounded-lg text-sm transition ${
      isActive(path)
        ? "bg-white/10 text-white font-medium"
        : "text-gray-300 hover:bg-white/5 hover:text-white"
    }`;

  const canManageEquipment = role === "Administrador" || role === "Técnico";

  return (
    <aside className="w-64 min-h-screen bg-[#0f1729] border-r border-white/10 flex flex-col p-4">
      <div className="mb-6">
        <span className="inline-block text-xs font-semibold tracking-wide text-blue-400 bg-blue-400/10 px-3 py-1 rounded-full uppercase">
          {role}
        </span>
      </div>

      <nav className="flex-1 space-y-6">
        <div>
          <p className="px-4 text-xs font-semibold text-gray-500 uppercase mb-1">General</p>
          <Link to="/dashboard" className={linkClass("/dashboard")}>
            Dashboard
          </Link>
        </div>

        <div>
          <p className="px-4 text-xs font-semibold text-gray-500 uppercase mb-1">Gestión</p>
          <Link to="/equipos" className={linkClass("/equipos")}>
            Equipos
          </Link>
          <Link to="/asignaciones" className={linkClass("/asignaciones")}>
            Asignaciones
          </Link>
          {canManageEquipment && (
            <Link to="/repuestos" className={linkClass("/repuestos")}>
              Repuestos
            </Link>
          )}
          {role === "Administrador" && (
            <Link to="/usuarios" className={linkClass("/usuarios")}>
              Usuarios
            </Link>
          )}
          {(role === "Administrador" || role === "Cliente") && (
            <Link to="/reportes" className={linkClass("/reportes")}>
              Reportes
            </Link>
          )}
        </div>
      </nav>

      <div className="border-t border-white/10 pt-4 mt-4">
        <p className="px-4 text-sm text-gray-300 mb-2 truncate">{fullName}</p>
        <button
          onClick={handleLogout}
          className="w-full text-left px-4 py-2 rounded-lg text-sm text-gray-300 hover:bg-white/5 hover:text-white"
        >
          Cerrar sesión
        </button>
      </div>
    </aside>
  );
}
