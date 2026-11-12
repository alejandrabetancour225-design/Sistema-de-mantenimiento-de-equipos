import axios from "axios";

export const api = axios.create({
  // "/api" lo reenvía nginx (contenedores) o el proxy de Vite (modo --dev) a la API.
  baseURL: import.meta.env.VITE_API_URL || "/api",
});

// Adjunta el token guardado a cada request (lo usaremos más adelante)
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // En /auth/* un 401 significa "credenciales incorrectas", no "sesión vencida":
    // ahí el formulario debe mostrar el error en vez de recargar la página.
    const isAuthCall = String(error.config?.url ?? "").includes("/auth/");
    if (error.response?.status === 401 && !isAuthCall) {
      localStorage.removeItem("token");
      localStorage.removeItem("role");
      localStorage.removeItem("fullName");
      window.location.hash = "#/";
      window.location.reload();
    }
    return Promise.reject(error);
  }
);