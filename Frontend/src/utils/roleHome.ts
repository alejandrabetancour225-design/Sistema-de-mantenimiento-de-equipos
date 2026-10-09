import type { Role } from "../types/auth";

// Pantalla de inicio según el rol: el Cliente solo puede consultar
// mantenimientos (ver Roles.cs en el backend); el resto entra por Equipos.
export function roleHome(role: Role | null): string {
  return role === "Cliente" ? "/mantenimientos" : "/equipos";
}