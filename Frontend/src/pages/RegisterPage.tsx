import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { z } from "zod";
import { register as registerUser } from "../services/authServices";
import { useAuth } from "../context/authContext";

const schema = z.object({
  fullName: z.string().min(1, "Requerido"),
  email: z.string().email("Correo inválido"),
  password: z.string().min(6, "Mínimo 6 caracteres"),
  phone: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

export default function RegisterPage() {
  const [values, setValues] = useState<FormValues>({
    fullName: "",
    email: "",
    password: "",
    phone: "",
  });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const { setSession } = useAuth();
  const navigate = useNavigate();

  function handleChange(field: keyof FormValues, value: string) {
    setValues((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    const parsed = schema.safeParse(values);
    if (!parsed.success) {
      setError(parsed.error.issues[0].message);
      return;
    }

    setLoading(true);
    try {
      // El backend ignora cualquier rol que se mande aquí: al primer usuario
      // del sistema lo hace Administrador automáticamente, y a todos los
      // siguientes les asigna el rol "Cliente" por defecto. Un Administrador
      // debe reasignar el rol después desde la gestión de usuarios.
      const data = await registerUser(parsed.data);
      setSession(data);
      navigate("/");
    } catch (err: any) {
      // El backend responde 409 con { message } si el correo ya está registrado.
      setError(err.response?.data?.message ?? "No se pudo completar el registro.");
    } finally {
      setLoading(false);
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

      {error && <p className="text-sm text-red-600">{error}</p>}

      <button
        type="submit"
        disabled={loading}
        className="w-full rounded bg-blue-600 py-2 text-white disabled:opacity-50"
      >
        {loading ? "Creando cuenta..." : "Registrarme"}
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