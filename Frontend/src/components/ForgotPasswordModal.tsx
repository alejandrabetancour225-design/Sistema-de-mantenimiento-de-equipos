import { useState } from "react";
import { forgotPassword, resetPassword } from "../services/authServices";
import { getApiErrorMessage } from "../lib/errors";

type Step = "email" | "code" | "done";

export default function ForgotPasswordModal({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const [step, setStep] = useState<Step>("email");
  const [email, setEmail] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  if (!open) return null;

  function reset() {
    setStep("email");
    setEmail("");
    setCode("");
    setNewPassword("");
    setError("");
    setLoading(false);
  }

  function handleClose() {
    reset();
    onClose();
  }

  async function handleRequestCode(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await forgotPassword(email);
      setStep("code");
    } catch {
      setError("No se pudo enviar el código. Intenta de nuevo.");
    } finally {
      setLoading(false);
    }
  }

  async function handleResetPassword(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await resetPassword(email, code, newPassword);
      setStep("done");
    } catch (err: unknown) {
        setError(getApiErrorMessage(err, "Código inválido o expirado."));
    } finally {
        setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-sm rounded-2xl border border-white/10 bg-[#111527] p-6 shadow-xl">
        {step === "email" && (
          <form onSubmit={handleRequestCode}>
            <h2 className="text-lg font-semibold text-white">Recuperar contraseña</h2>
            <p className="mt-2 text-sm text-gray-400">
              Ingresa tu correo y te enviaremos un código de 6 dígitos.
            </p>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="tu@correo.com"
              className="mt-4 w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
            {error && <p className="mt-2 text-xs text-red-400">{error}</p>}
            <div className="mt-5 flex gap-2">
              <button
                type="button"
                onClick={handleClose}
                className="flex-1 rounded-lg border border-white/10 py-2 text-sm text-gray-300 hover:bg-white/5"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={loading}
                className="flex-1 rounded-lg bg-white py-2 text-sm font-medium text-[#0a0e1a] hover:bg-gray-200 disabled:opacity-50"
              >
                {loading ? "Enviando..." : "Enviar código"}
              </button>
            </div>
          </form>
        )}

        {step === "code" && (
          <form onSubmit={handleResetPassword}>
            <h2 className="text-lg font-semibold text-white">Ingresa el código</h2>
            <p className="mt-2 text-sm text-gray-400">
              Enviamos un código a <strong>{email}</strong>. Expira en 15 minutos.
            </p>
            <input
              type="text"
              required
              maxLength={6}
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
              placeholder="123456"
              className="mt-4 w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-center text-lg tracking-widest text-white"
            />
            <input
              type="password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Nueva contraseña"
              className="mt-3 w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
            {error && <p className="mt-2 text-xs text-red-400">{error}</p>}
            <div className="mt-5 flex gap-2">
              <button
                type="button"
                onClick={() => setStep("email")}
                className="flex-1 rounded-lg border border-white/10 py-2 text-sm text-gray-300 hover:bg-white/5"
              >
                Atrás
              </button>
              <button
                type="submit"
                disabled={loading}
                className="flex-1 rounded-lg bg-white py-2 text-sm font-medium text-[#0a0e1a] hover:bg-gray-200 disabled:opacity-50"
              >
                {loading ? "Verificando..." : "Restablecer"}
              </button>
            </div>
          </form>
        )}

        {step === "done" && (
          <div>
            <h2 className="text-lg font-semibold text-white">¡Listo!</h2>
            <p className="mt-2 text-sm text-gray-400">
              Tu contraseña fue restablecida. Ya puedes iniciar sesión con la nueva.
            </p>
            <button
              onClick={handleClose}
              className="mt-5 w-full rounded-lg bg-white py-2 text-sm font-medium text-[#0a0e1a] hover:bg-gray-200"
            >
              Entendido
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
