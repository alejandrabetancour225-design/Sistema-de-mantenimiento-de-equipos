import { api } from "./api";
import type { UserResponse, UpdateUserRequest } from "../types/user";
import type { Role } from "../types/auth";

export async function getUsers(): Promise<UserResponse[]> {
  const res = await api.get<UserResponse[]>("/users");
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