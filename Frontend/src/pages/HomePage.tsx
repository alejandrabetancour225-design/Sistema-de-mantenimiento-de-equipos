import { Link } from "react-router-dom";

export default function HomePage() {
  return (
    <div className="min-h-[calc(100vh-72px)] flex flex-col items-center justify-center bg-gray-50 text-center px-4">
      <h1 className="text-3xl font-bold mb-4">Sistema de Gestión de Mantenimiento</h1>
      <p className="text-gray-600 mb-8 max-w-md">
        Administra equipos, mantenimientos y asignaciones en un solo lugar.
      </p>
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