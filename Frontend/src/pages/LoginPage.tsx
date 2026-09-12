import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useNavigate, Link } from "react-router-dom";
import { loginSchema, type LoginFormData } from "../schemas/authSchemas";
import { useLogin, getAuthErrorMessage } from "../hooks/useAuthMutations";

export default function LoginPage() {
  const navigate = useNavigate();
  const loginMutation = useLogin();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({ resolver: zodResolver(loginSchema) });

  const onSubmit = (data: LoginFormData) => {
    loginMutation.mutate(data, { onSuccess: () => navigate("/dashboard") });
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <form
        onSubmit={handleSubmit(onSubmit)}
        className="w-full max-w-sm bg-white p-8 rounded-lg shadow"
      >
        <h1 className="text-2xl font-semibold mb-6">Iniciar sesión</h1>

        <label className="block mb-1 text-sm font-medium">Correo</label>
        <input
          type="email"
          {...register("email")}
          className="w-full mb-1 border rounded px-3 py-2"
        />
        {errors.email && (
          <p className="text-red-500 text-sm mb-2">{errors.email.message}</p>
        )}

        <label className="block mb-1 text-sm font-medium mt-3">Contraseña</label>
        <input
          type="password"
          {...register("password")}
          className="w-full mb-1 border rounded px-3 py-2"
        />
        {errors.password && (
          <p className="text-red-500 text-sm mb-2">{errors.password.message}</p>
        )}

        {loginMutation.isError && (
          <p className="text-red-600 text-sm mt-2">
            {getAuthErrorMessage(loginMutation.error)}
          </p>
        )}

        <button
          type="submit"
          disabled={loginMutation.isPending}
          className="w-full mt-4 bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-50"
        >
          {loginMutation.isPending ? "Ingresando..." : "Ingresar"}
        </button>

        <p className="text-sm text-center mt-4">
          ¿No tienes cuenta?{" "}
          <Link to="/register" className="text-blue-600">
            Regístrate
          </Link>
        </p>
      </form>
    </div>
  );
}