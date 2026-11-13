import type { Role } from "../types/auth";

// El SuperAdmin administra la plataforma (organizaciones); los demás roles inician en su dashboard.
export function roleHome(role: Role | null): string {
  return role === "SuperAdmin" ? "/organizaciones" : "/dashboard";
}
