import { createContext, useContext, useState, type ReactNode } from "react";
import type { AuthResponse } from "../types/auth";

interface AuthUser {
  name: string;
  email: string;
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  setSession: (data: AuthResponse) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const name = localStorage.getItem("userName");
    const email = localStorage.getItem("userEmail");
    return name && email ? { name, email } : null;
  });

  const setSession = (data: AuthResponse) => {
    localStorage.setItem("token", data.token);
    localStorage.setItem("userName", data.name);
    localStorage.setItem("userEmail", data.email);
    setUser({ name: data.name, email: data.email });
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, setSession, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuthContext() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuthContext debe usarse dentro de AuthProvider");
  return ctx;
}