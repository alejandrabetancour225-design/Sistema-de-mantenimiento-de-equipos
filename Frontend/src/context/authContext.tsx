import { createContext, useContext, useState, type ReactNode } from "react";
import type { AuthResponse, Role } from "../types/auth";

interface AuthState {
  token: string | null;
  fullName: string | null;
  role: Role | null;
  isAuthenticated: boolean;
  setSession: (data: AuthResponse) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem("token"));
  const [role, setRole] = useState<Role | null>(
    () => (localStorage.getItem("role") as Role | null) ?? null
  );
  const [fullName, setFullName] = useState<string | null>(
    () => localStorage.getItem("fullName")
  );

  function setSession(data: AuthResponse) {
    localStorage.setItem("token", data.token);
    if (data.role) localStorage.setItem("role", data.role);
    localStorage.setItem("fullName", data.fullName);
    setToken(data.token);
    setRole(data.role ?? null);
    setFullName(data.fullName);
  }

  function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    localStorage.removeItem("fullName");
    setToken(null);
    setRole(null);
    setFullName(null);
  }

  return (
    <AuthContext.Provider
      value={{ token, role, fullName, isAuthenticated: !!token, setSession, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth debe usarse dentro de <AuthProvider>");
  return ctx;
}