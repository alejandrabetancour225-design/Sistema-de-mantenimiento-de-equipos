export type Role ="Administrator" | "Técnico" | "Empleado" | "Cliente";
export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phone?: string;
}

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
}