import { Navigate } from "react-router-dom";
import { useAuth } from "../context/authContext";
import LoginFeatures from "../components/LoginFeatures";
import LoginCard from "../components/LoginCard";

export default function LoginPage() {
  const { isAuthenticated } = useAuth();

  if (isAuthenticated) return <Navigate to="/equipos" replace />;

  return (
    <div className="relative min-h-screen overflow-hidden bg-[#0a0e1a]">
      {/* Fondo con gradiente radial oscuro, tal como el modo dark de la plantilla */}
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          backgroundImage:
            "radial-gradient(at 50% 50%, hsla(210, 100%, 16%, 0.5), hsl(220, 30%, 5%))",
        }}
      />

      <div className="mx-auto flex min-h-screen max-w-5xl flex-col-reverse items-center justify-center gap-12 px-4 py-10 md:flex-row md:gap-16">
        <LoginFeatures />
        <LoginCard />
      </div>
    </div>
  );
}