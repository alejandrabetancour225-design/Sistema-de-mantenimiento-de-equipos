import { createContext, useContext, useState, type ReactNode } from "react";
import type { AuthResponse, Role } from "../types/auth";

interface AuthState {
  token: string | null;
  userId: string | null;
  fullName: string | null;
  role: Role | null;
  isAuthenticated: boolean;
  setSession: (data: AuthResponse) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

// El token solo trae el claim "sub" (ver TokenService.cs del backend) con el
// id del usuario; lo decodificamos en el cliente sin librerías extra.
function decodeUserId(token: string): string | null {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/")));
    return json.sub ?? null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem("token"));
  const [userId, setUserId] = useState<string | null>(() => {
    const t = localStorage.getItem("token");
    return t ? decodeUserId(t) : null;
  });
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
    setUserId(decodeUserId(data.token));
    setRole(data.role ?? null);
    setFullName(data.fullName);
  }

  function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    localStorage.removeItem("fullName");
    setToken(null);
    setUserId(null);
    setRole(null);
    setFullName(null);
  }

  return (
    <AuthContext.Provider
      value={{ token, userId, role, fullName, isAuthenticated: !!token, setSession, logout }}
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