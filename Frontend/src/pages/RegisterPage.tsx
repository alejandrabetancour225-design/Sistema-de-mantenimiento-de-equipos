import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { registerSchema, type RegisterFormData } from "../schemas/authSchemas";
import { useRegister, getAuthErrorMessage } from "../hooks/useAuthMutations";

export default function RegisterPage() {
  const [values, setValues] = useState<RegisterFormData>({
    fullName: "",
    email: "",
    password: "",
    phone: "",
  });
  const [validationError, setValidationError] = useState<string | null>(null);
  const { mutate, isPending, error } = useRegister();
  const navigate = useNavigate();

  function handleChange(field: keyof RegisterFormData, value: string) {
    setValues((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setValidationError(null);

    const parsed = registerSchema.safeParse(values);
    if (!parsed.success) {
      setValidationError(parsed.error.issues[0].message);
      return;
    }

    try {
      // El backend ignora cualquier rol que se mande aquí: al primer usuario
      // del sistema lo hace Administrador automáticamente, y a todos los
      // siguientes les asigna el rol "Cliente" por defecto. Un Administrador
      // debe reasignar el rol después desde la gestión de usuarios.
      await mutate(parsed.data);
      navigate("/");
    } catch {
      // el error ya queda disponible en `error`, no hace falta hacer nada más aquí
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto max-w-sm space-y-4 p-8">
      <h1 className="text-xl font-semibold">Crear cuenta</h1>

      <div>
        <label className="block text-sm font-medium">Nombre completo</label>
        <input
          value={values.fullName}
          onChange={(e) => handleChange("fullName", e.target.value)}
          className="input"
        />
      </div>

      <div>
        <label className="block text-sm font-medium">Correo</label>
        <input
          type="email"
          value={values.email}
          onChange={(e) => handleChange("email", e.target.value)}
          className="input"
        />
      </div>

      <div>
        <label className="block text-sm font-medium">Contraseña</label>
        <input
          type="password"
          value={values.password}
          onChange={(e) => handleChange("password", e.target.value)}
          className="input"
        />
      </div>

      <div>
        <label className="block text-sm font-medium">Teléfono (opcional)</label>
        <input
          value={values.phone}
          onChange={(e) => handleChange("phone", e.target.value)}
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
        {isPending ? "Creando cuenta..." : "Registrarme"}
      </button>

      <p className="text-center text-sm">
        ¿Ya tienes cuenta?{" "}
        <Link to="/login" className="text-blue-600">
          Inicia sesión
        </Link>
      </p>
    </form>
  );
}