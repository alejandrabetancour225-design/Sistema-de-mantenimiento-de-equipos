import { Link } from "react-router-dom";
import { useAuth } from "../context/authContext";

export default function HomePage() {
  const { isAuthenticated, fullName } = useAuth();

  return (
    <div className="min-h-[calc(100vh-72px)] flex flex-col items-center justify-center bg-gray-50 text-center px-4">
     

      {isAuthenticated ? (
        <div className="space-y-4">
          <h1 className="text-2xl font-bold text-gray-700">¡Hola {fullName}!</h1>
          <p></p>
          <Link
            to="/equipos"
            className="bg-blue-600 text-white px-6 py-2 rounded hover:bg-blue-700"
          >
            Ir a Equipos
          </Link>
        </div>
      ) : (
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
      )}
    </div>
  );
}