import { Navigate, Outlet } from "react-router-dom";
import { useAuthContext } from "../context/authContext";

export default function ProtectedRoute() {
  const { isAuthenticated } = useAuthContext();
  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
