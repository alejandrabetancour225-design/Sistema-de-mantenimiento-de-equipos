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
  <div className="relative min-h-screen overflow-hidden bg-[#0a0e1a]">

    {/* Fondo con gradiente */}
    <div
      className="pointer-events-none absolute inset-0 -z-10"
      style={{
        backgroundImage:
          "radial-gradient(at 50% 50%, hsla(210, 100%, 16%, 0.5), hsl(220, 30%, 5%))",
      }}
    />

    {/* Contenido */}
    <div className="flex min-h-screen items-center justify-center px-4 py-10">

      <form
        onSubmit={handleSubmit}
        className="w-full max-w-[450px] rounded-2xl border border-white/10 bg-[#111527] p-8 shadow-[0_5px_15px_rgba(0,0,0,0.5),0_15px_35px_-5px_rgba(0,0,0,0.3)]"
      >

        <h1 className="text-[2.15rem] font-bold text-white">
          Crear cuenta
        </h1>

        {/* Nombre */}
        <div className="mt-6">
          <label className="mb-1.5 block text-sm font-medium text-gray-300">
            Nombre completo
          </label>

          <input
            value={values.fullName}
            onChange={(e) =>
              handleChange("fullName", e.target.value)
            }
            className="w-full rounded-lg border border-white/10 bg-[#0a0e1a] px-3 py-2.5 text-sm text-white outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
          />
        </div>

        {/* Correo */}
        <div className="mt-4">
          <label className="mb-1.5 block text-sm font-medium text-gray-300">
            Correo
          </label>

          <input
            type="email"
            value={values.email}
            onChange={(e) =>
              handleChange("email", e.target.value)
            }
            className="w-full rounded-lg border border-white/10 bg-[#0a0e1a] px-3 py-2.5 text-sm text-white outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
          />
        </div>

        {/* Contraseña */}
        <div className="mt-4">
          <label className="mb-1.5 block text-sm font-medium text-gray-300">
            Contraseña
          </label>

          <input
            type="password"
            value={values.password}
            onChange={(e) =>
              handleChange("password", e.target.value)
            }
            className="w-full rounded-lg border border-white/10 bg-[#0a0e1a] px-3 py-2.5 text-sm text-white outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
          />
        </div>

        {/* Teléfono */}
        <div className="mt-4">
          <label className="mb-1.5 block text-sm font-medium text-gray-300">
            Teléfono (opcional)
          </label>

          <input
            value={values.phone}
            onChange={(e) =>
              handleChange("phone", e.target.value)
            }
            className="w-full rounded-lg border border-white/10 bg-[#0a0e1a] px-3 py-2.5 text-sm text-white outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
          />
        </div>

        {/* Error */}
        {(validationError || !!error) && (
          <p className="mt-4 text-sm text-red-400">
            {validationError ?? getAuthErrorMessage(error)}
          </p>
        )}

        {/* Botón */}
        <button
          type="submit"
          disabled={isPending}
          className="mt-4 w-full rounded-lg bg-white py-2.5 text-sm font-medium text-[#0a0e1a] transition hover:bg-blue-500 hover:text-white disabled:opacity-50"
        >
          {isPending ? "Creando cuenta..." : "Registrarme"}
        </button>

        {/* Login */}
        <p className="mt-4 text-center text-sm text-gray-400">
          ¿Ya tienes cuenta?{" "}
          <Link
            to="/login"
            className="font-medium text-blue-400 hover:underline"
          >
            Inicia sesión
          </Link>
        </p>

      </form>
    </div>
  </div>
  );
}