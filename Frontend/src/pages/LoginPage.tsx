import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { z } from "zod";
import { login } from "../services/authServices";
import { useAuth } from "../context/authContext";

const schema = z.object({
  email: z.string().email("Correo inválido"),
  password: z.string().min(1, "La contraseña es requerida"),
});

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const { setSession } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    const parsed = schema.safeParse({ email, password });
    if (!parsed.success) {
      setError(parsed.error.issues[0].message);
      return;
    }

    setLoading(true);
    try {
      const data = await login(parsed.data);
      setSession(data);
      navigate("/");
    } catch (err: any) {
      // El backend responde 401 con { message } si las credenciales son
      // inválidas o la cuenta quedó bloqueada tras varios intentos fallidos.
      setError(err.response?.data?.message ?? "No se pudo iniciar sesión.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto max-w-sm space-y-4 p-8">
      <h1 className="text-xl font-semibold">Iniciar sesión</h1>

      <div>
        <label className="block text-sm font-medium">Correo</label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="input"
        />
      </div>

      <div>
        <label className="block text-sm font-medium">Contraseña</label>
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="input"
        />
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <button
        type="submit"
        disabled={loading}
        className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50"
      >
        {loading ? "Ingresando..." : "Ingresar"}
      </button>

      <p className="text-center text-sm">
        ¿No tienes cuenta?{" "}
        <Link to="/register" className="text-blue-600">
          Regístrate
        </Link>
      </p>
    </form>
  );
}