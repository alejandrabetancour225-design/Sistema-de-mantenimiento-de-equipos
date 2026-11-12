import { api } from "./api";
import type { CreateUserRequest, UserResponse, UpdateUserRequest } from "../types/user";
import type { Role } from "../types/auth";

export async function getUsers(): Promise<UserResponse[]> {
  const res = await api.get<UserResponse[]>("/users");
  return res.data;
}

export async function createUser(data: CreateUserRequest): Promise<UserResponse> {
  const res = await api.post<UserResponse>("/users", data);
  return res.data;
}

export async function resetUserPassword(id: string, newPassword: string): Promise<UserResponse> {
  const res = await api.put<UserResponse>(`/users/${id}/password`, { newPassword });
  return res.data;
}

export async function updateUser(id: string, data: UpdateUserRequest): Promise<UserResponse> {
  const res = await api.put<UserResponse>(`/users/${id}`, data);
  return res.data;
}

export async function assignRole(id: string, role: Role): Promise<UserResponse> {
  const res = await api.put<UserResponse>(`/users/${id}/role`, { role });
  return res.data;
}