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