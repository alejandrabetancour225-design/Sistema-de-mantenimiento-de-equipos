import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Settings } from "lucide-react";
import { loginSchema } from "../schemas/authSchemas";
import { useLogin, getAuthErrorMessage } from "../hooks/useAuthMutations";
import ForgotPasswordModal from "./ForgotPasswordModal";

export default function LoginCard() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [forgotOpen, setForgotOpen] = useState(false);
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
      navigate("/equipos");
    } catch {
      // el error ya queda disponible en `error`
    }
  }

  return (
    <div className="w-full max-w-[450px] rounded-2xl border border-white/10 bg-[#111527] p-8 shadow-[0_5px_15px_rgba(0,0,0,0.5),0_15px_35px_-5px_rgba(0,0,0,0.3)]">
      <div className="mb-2 flex items-center gap-2 md:hidden">
        <Settings className="h-6 w-6 text-blue-400" />
        <span className="text-lg font-semibold text-white">Sistema de Mantenimiento</span>
      </div>

      <h1 className="text-[clamp(2rem,10vw,2.15rem)] font-bold text-white">Iniciar sesión</h1>

      <form onSubmit={handleSubmit} className="mt-6 flex flex-col gap-4">
        <div>
          <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-gray-300">
            Email
          </label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="your@email.com"
            autoComplete="email"
            autoFocus
            required
            className="w-full rounded-lg border border-white/10 bg-[#0a0e1a] px-3 py-2.5 text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>

        <div>
          <div className="mb-1.5 flex items-center justify-between">
            <label htmlFor="password" className="text-sm font-medium text-gray-300">
              Password
            </label>
            <button
              type="button"
              onClick={() => setForgotOpen(true)}
              className="text-sm text-blue-400 hover:underline"
            >
              ¿Olvidaste tu contraseña?
            </button>
          </div>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••"
            autoComplete="current-password"
            required
            className="w-full rounded-lg border border-white/10 bg-[#0a0e1a] px-3 py-2.5 text-sm text-white focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>

        {(validationError || Boolean(error)) && (
          <p className="text-sm text-red-400">
            {validationError ?? getAuthErrorMessage(error)}
          </p>
        )}

        <button
          type="submit"
          disabled={isPending}
          className="w-full rounded-lg bg-white py-2.5 text-sm font-medium text-[#0a0e1a] transition hover:bg-blue-500 hover:text-white disabled:opacity-50"
        >
          {isPending ? "Ingresando..." : "Iniciar sesión"}
        </button>

        <p className="text-center text-sm text-gray-400">
          ¿No tienes una cuenta?{" "}
          <Link to="/register" className="font-medium text-blue-400 hover:underline">
            Registrarse
          </Link>
        </p>
      </form>

      <ForgotPasswordModal open={forgotOpen} onClose={() => setForgotOpen(false)} />
    </div>
  );
}