import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { loginSchema } from "../schemas/authSchemas";
import { useLogin, getAuthErrorMessage } from "../hooks/useAuthMutations";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const { mutate, isPending, error } = useLogin();
  const navigate = useNavigate();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setValidationError(null);

    const parsed = loginSchema.safeParse({ email, password });
    if (!parsed.success) {
      setValidationError(parsed.error.issues[0].message);
      return;
    }

    try {
      await mutate(parsed.data);
      navigate("/");
    } catch {
      // el error ya queda disponible en `error`, no hace falta hacer nada más aquí
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

      {(validationError || !!error) && (
        <p className="text-sm text-red-600">
          {validationError ?? getAuthErrorMessage(error)}
        </p>
      )}

      <button
        type="submit"
        disabled={isPending}
        className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50"
      >
        {isPending ? "Ingresando..." : "Ingresar"}
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