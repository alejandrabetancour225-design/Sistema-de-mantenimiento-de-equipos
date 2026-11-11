import type { Role } from "../types/auth";

// Todos los roles inician en el Dashboard; cada rol ve su propia versión.
// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function roleHome(_role: Role | null): string {
  return "/dashboard";
}