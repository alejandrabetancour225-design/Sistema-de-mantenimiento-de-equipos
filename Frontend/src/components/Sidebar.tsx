import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/authContext";

export default function Sidebar() {
  const { isAuthenticated, role, fullName, organizationName, plan, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  if (!isAuthenticated) return null;

  function handleLogout() {
    logout();
    navigate("/");
  }

  const isActive = (path: string) => location.pathname === path;
  const linkClass = (path: string) =>
    `block px-4 py-2 rounded-lg text-sm transition ${
      isActive(path)
        ? "bg-white/10 text-white font-medium"
        : "text-gray-300 hover:bg-white/5 hover:text-white"
    }`;

  // Personal interno = todos los roles menos Gerente (Roles.Staff en el backend)
  const isStaff = role !== "Gerente";
  // Reportes: Administrador y Gerente (mismos roles que el backend). El dashboard lo ven todos.
  const canSeeDashboard = role === "Administrador" || role === "Gerente";
  const canManageEquipment = role === "Administrador" || role === "Técnico";

  if (role === "SuperAdmin") {
    return (
      <aside className="w-64 min-h-screen bg-[#0f1729] border-r border-white/10 flex flex-col p-4">
        <span className="mb-6 inline-block w-fit text-xs font-semibold tracking-wide text-blue-400 bg-blue-400/10 px-3 py-1 rounded-full uppercase">
          Plataforma
        </span>
        <nav className="flex-1">
          <Link to="/organizaciones" className={linkClass("/organizaciones")}>
            Organizaciones
          </Link>
          <Link to="/cambiar-contrasena" className={linkClass("/cambiar-contrasena")}>
            Cambiar contraseña
          </Link>
        </nav>
        <div className="border-t border-white/10 pt-4 mt-4">
          <p className="px-4 text-sm text-gray-300 mb-2 truncate">{fullName}</p>
          <button onClick={handleLogout} className="w-full text-left px-4 py-2 rounded-lg text-sm text-gray-300 hover:bg-white/5 hover:text-white">
            Cerrar sesión
          </button>
        </div>
      </aside>
    );
  }

  return (
    <aside className="w-64 min-h-screen bg-[#0f1729] border-r border-white/10 flex flex-col p-4">
      <div className="mb-6">
        <span className="inline-block text-xs font-semibold tracking-wide text-blue-400 bg-blue-400/10 px-3 py-1 rounded-full uppercase">
          {role}
        </span>
        {organizationName && (
          <p className="mt-2 px-1 text-xs text-gray-400">
            {organizationName} · Plan {plan === "INTEGRACIONES" ? "Integraciones" : "Básico"}
          </p>
        )}
      </div>

      <nav className="flex-1 space-y-6">
        <div>
          <p className="px-4 text-xs font-semibold text-gray-500 uppercase mb-1">General</p>
          <Link to="/dashboard" className={linkClass("/dashboard")}>
            Dashboard
          </Link>
          {canManageEquipment ? (
            <Link to="/ai-metrics" className={linkClass("/ai-metrics")}>
              Métricas IA
            </Link>
          ) : null}
        </div>

        <div>
          <p className="px-4 text-xs font-semibold text-gray-500 uppercase mb-1">Gestión</p>
          {isStaff && (
            <>
              <Link to="/equipos" className={linkClass("/equipos")}>
                Equipos
              </Link>
              <Link to="/asignaciones" className={linkClass("/asignaciones")}>
                Asignaciones
              </Link>
              {(role === "Empleado" || role === "Administrador") && (
                <Link to="/asistente" className={linkClass("/asistente")}>
                  Reportar una falla
                </Link>
              )}
              {canManageEquipment && (
                <Link to="/diagnosis" className={linkClass("/diagnosis")}>
                  Diagnóstico IA
                </Link>
              )}
            </>
          )}
          <Link to="/mantenimientos" className={linkClass("/mantenimientos")}>
            Mantenimientos
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
          {canSeeDashboard && (
            <Link to="/reportes" className={linkClass("/reportes")}>
              Reportes
            </Link>
          )}
          {role === "Administrador" && (
            <Link to="/auditoria" className={linkClass("/auditoria")}>
              Auditoría
            </Link>
          )}
          {canManageEquipment && (
            <Link to="/error-knowledge" className={linkClass("/error-knowledge")}>
              Biblioteca IA
            </Link>
          )}
          {isStaff && (
            <Link to="/glosario" className={linkClass("/glosario")}>
              Glosario de modismos
            </Link>
          )}
        </div>
      </nav>

      <div className="border-t border-white/10 pt-4 mt-4">
        <p className="px-4 text-sm text-gray-300 mb-2 truncate">{fullName}</p>
        <Link to="/cambiar-contrasena" className={linkClass("/cambiar-contrasena")}>
          Cambiar contraseña
        </Link>
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
