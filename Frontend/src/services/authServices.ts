import { api } from "./api";
import type { LoginRequest, RegisterRequest, AuthResponse } from "../types/auth";

export async function login(data: LoginRequest): Promise<AuthResponse> {
  const res = await api.post<AuthResponse>("/auth/login", data);
  return res.data;
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const res = await api.post<AuthResponse>("/auth/register", data);
  return res.data;
}

export async function forgotPassword(email: string): Promise<void> {
  await api.post("/auth/forgot-password", { email });
}

export async function resetPassword(email: string, code: string, newPassword: string): Promise<void> {
  await api.post("/auth/reset-password", { email, code, newPassword });
}
