import type { Role } from "../types/auth";
import type { EquipmentStatus } from "../types/equipment";

// Espejo en la interfaz de la regla maestra R4 (EquipmentStateMachine en el backend).
// A mano solo se puede: marcar Fuera de servicio, devolver a servicio y (Administrador) dar de baja.
// Disponible/En uso los ponen las asignaciones y En mantenimiento lo ponen los mantenimientos.
export interface StatusAction {
  target: EquipmentStatus;
  label: string;
  kind: "status" | "decommission";
}

export function manualStatusActions(
  status: EquipmentStatus,
  role: Role | null,
  ownership: "OWNED" | "RENTED" = "OWNED"
): StatusAction[] {
  const isAdmin = role === "Administrador";
  const actions: StatusAction[] = [];

  if (status === "AVAILABLE" || status === "IN_USE") {
    actions.push({ target: "OUT_OF_SERVICE", label: "Marcar fuera de servicio", kind: "status" });
  }
  if (status === "OUT_OF_SERVICE") {
    // La API decide si vuelve a Disponible o En uso según la asignación activa.
    actions.push({ target: "AVAILABLE", label: "Devolver a servicio", kind: "status" });
  }
  // Un equipo alquilado no se da de baja: se devuelve al arrendador (página Alquiler / detalle).
  if (isAdmin && ownership === "OWNED" && (status === "AVAILABLE" || status === "OUT_OF_SERVICE")) {
    actions.push({ target: "DECOMMISSIONED", label: "Dar de baja", kind: "decommission" });
  }
  return actions;
}

export function statusLockedReason(status: EquipmentStatus): string | null {
  switch (status) {
    case "UNDER_MAINTENANCE":
      return "Cambia solo al cerrar o cancelar el mantenimiento abierto.";
    case "DECOMMISSIONED":
      return "Un equipo dado de baja no vuelve a servicio.";
    case "RETURNED":
      return "Equipo devuelto al arrendador.";
    case "IN_USE":
      return "Para dejarlo Disponible, libera su asignación.";
    default:
      return null;
  }
}
