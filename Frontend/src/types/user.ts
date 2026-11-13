import type { Role } from "./auth";

export interface UserResponse {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  active: boolean;
  role?: Role;
  failedAttempts: number;
  mustChangePassword?: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateUserRequest {
  fullName?: string;
  email?: string;
  phone?: string;
  active?: boolean;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  role: Role;
  temporaryPassword: string;
  phone?: string;
}

export interface AssignRoleRequest {
  role: Role;
}