import type { Role } from "./auth";

export interface UserResponse {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  active: boolean;
  role?: Role;
  failedAttempts: number;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateUserRequest {
  fullName?: string;
  email?: string;
  phone?: string;
  active?: boolean;
}

export interface AssignRoleRequest {
  role: Role;
}