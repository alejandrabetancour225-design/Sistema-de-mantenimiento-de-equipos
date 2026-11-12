import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { changePassword } from "../services/authServices";
import { useAuth } from "../context/authContext";
import { strongPassword } from "../schemas/authSchemas";
import { getApiErrorMessage } from "../lib/errors";
import { roleHome } from "../utils/roleHome";

// Cambio de contraseña. Obligatorio cuando la cuenta tiene una contraseña temporal.
export default function ChangePasswordPage() {
  const { setSession, mustChangePassword, role } = useAuth();
  const navigate = useNavigate();
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    const check = strongPassword.safeParse(next);
    if (!check.success) return setError(check.error.issues[0].message);
    if (next !== confirm) return setError("Las contraseñas nuevas no coinciden.");
    if (next === current) return setError("La contraseña nueva debe ser distinta de la actual.");
    setSaving(true);
    try {
      const session = await changePassword(current, next);
      setSession(session);
      navigate(roleHome(role), { replace: true });
    } catch (err) {
      setError(getApiErrorMessage(err, "No se pudo cambiar la contraseña."));
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={submit} className="mx-auto max-w-sm space-y-3 p-8">
      <h1 className="text-xl font-semibold">Cambiar contraseña</h1>
      {mustChangePassword && (
        <p className="rounded bg-amber-500/10 p-2 text-sm text-amber-500">
          Su cuenta tiene una contraseña temporal. Debe cambiarla para continuar.
        </p>
      )}
      <input type="password" value={current} onChange={(e) => setCurrent(e.target.value)}
        placeholder="Contraseña actual" className="input w-full" required autoComplete="current-password" />
      <input type="password" value={next} onChange={(e) => setNext(e.target.value)}
        placeholder="Contraseña nueva" className="input w-full" required autoComplete="new-password" />
      <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)}
        placeholder="Repita la contraseña nueva" className="input w-full" required autoComplete="new-password" />
      <p className="text-xs text-gray-400">Entre 10 y 128 caracteres, con al menos una letra y un número.</p>
      {error && <p className="text-sm text-red-500">{error}</p>}
      <button type="submit" disabled={saving} className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50">
        {saving ? "Guardando…" : "Cambiar contraseña"}
      </button>
    </form>
  );
}
