import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, Link } from "react-router-dom";
import { registerSchema, type RegisterFormData } from "../schemas/authSchemas";
import { useRegister, getAuthErrorMessage } from "../hooks/useAuthMutations";

export default function RegisterPage() {
  const navigate = useNavigate();
  const registerMutation = useRegister();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({ resolver: zodResolver(registerSchema) });

  const onSubmit = (data: RegisterFormData) => {
    registerMutation.mutate(data, { onSuccess: () => navigate("/dashboard") });
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <form
        onSubmit={handleSubmit(onSubmit)}
        className="w-full max-w-sm bg-white p-8 rounded-lg shadow"
      >
        <h1 className="text-2xl font-semibold mb-6">Crear cuenta</h1>

        <label className="block mb-1 text-sm font-medium">Nombre</label>
        <input {...register("name")} className="w-full mb-1 border rounded px-3 py-2" />
        {errors.name && <p className="text-red-500 text-sm mb-2">{errors.name.message}</p>}

        <label className="block mb-1 text-sm font-medium mt-3">Correo</label>
        <input
          type="email"
          {...register("email")}
          className="w-full mb-1 border rounded px-3 py-2"
        />
        {errors.email && <p className="text-red-500 text-sm mb-2">{errors.email.message}</p>}

        <label className="block mb-1 text-sm font-medium mt-3">Contraseña</label>
        <input
          type="password"
          {...register("password")}
          className="w-full mb-1 border rounded px-3 py-2"
        />
        {errors.password && (
          <p className="text-red-500 text-sm mb-2">{errors.password.message}</p>
        )}

        {registerMutation.isError && (
          <p className="text-red-600 text-sm mt-2">
            {getAuthErrorMessage(registerMutation.error)}
          </p>
        )}

        <button
          type="submit"
          disabled={registerMutation.isPending}
          className="w-full mt-4 bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {registerMutation.isPending ? "Creando..." : "Crear cuenta"}
        </button>

        <p className="text-sm text-center mt-4">
          ¿Ya tienes cuenta?{" "}
          <Link to="/login" className="text-blue-600">
            Inicia sesión
          </Link>
        </p>
      </form>
    </div>
  );
}