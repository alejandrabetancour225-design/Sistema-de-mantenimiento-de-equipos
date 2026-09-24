import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/authContext";

export default function Navbar() {
  const { isAuthenticated, role, fullName, logout } = useAuth();
  const navigate = useNavigate();

  // Sin sesión no se muestra nada: la pantalla de inicio (login) ocupa toda la vista.
  if (!isAuthenticated) return null;

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <nav className="w-full bg-blue-600 text-white px-6 py-4 flex items-center justify-between">
      <Link to="/" className="font-semibold text-lg">
        Sistema de Mantenimiento
      </Link>

      <div className="flex items-center gap-4 text-sm">
        <Link to="/equipos">Equipos</Link>
        <Link to="/asignaciones">Asignaciones</Link>
        {role === "Administrador" && <Link to="/usuarios">Usuarios</Link>}
        <span className="opacity-75">{fullName}</span>
        <button onClick={handleLogout} className="underline">
          Salir
        </button>
      </div>
    </nav>
  );
}