
export type Role = "SuperAdmin" | "Administrador" | "Técnico" | "Empleado" | "Gerente";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
  role?: Role;
  active: boolean;
  organizationId?: string | null;
  organizationName?: string | null;
  plan?: "BASICO" | "INTEGRACIONES" | null;
  mustChangePassword?: boolean;
}