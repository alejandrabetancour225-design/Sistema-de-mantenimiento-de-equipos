import type { EquipmentStatus } from "./equipment";
import type { Incident, IncidentStatus } from "./incident";

export type MaintenanceType = "PREVENTIVE" | "CORRECTIVE";
export type MaintenanceStatus = "OPEN" | "IN_PROGRESS" | "COMPLETED" | "CANCELLED" | "VOIDED";

export const MAINTENANCE_TYPE_LABEL: Record<MaintenanceType, string> = {
  PREVENTIVE: "Preventivo",
  CORRECTIVE: "Correctivo",
};

export const MAINTENANCE_STATUS_LABEL: Record<MaintenanceStatus, string> = {
  OPEN: "Abierto",
  IN_PROGRESS: "En progreso",
  COMPLETED: "Completado",
  CANCELLED: "Cancelado",
  VOIDED: "Anulado",
};

export interface MaintenanceSparePart {
  id: string;
  sparePartId: string;
  sparePartName: string;
  quantity: number;
  unitCostAtUse: number;
  subtotal: number;
}

export interface Maintenance {
  id: string;
  equipmentId: string;
  equipmentInternalCode?: string | null;
  incidentId?: string | null;
  technicianId: string;
  technicianName?: string | null;
  type: MaintenanceType;
  status: MaintenanceStatus;
  reportedProblem: string;
  workDone?: string | null;
  laborCost: number;
  otherCosts: number;
  sparePartsCost: number;
  totalCost: number;
  nextMaintenanceDate?: string | null;
  observations: string;
  startedAt: string;
  completedAt?: string | null;
  // Anulación de un mantenimiento cerrado con errores (se corrige registrando uno nuevo).
  voidReason?: string | null;
  voidedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  spareParts: MaintenanceSparePart[];
}

export interface CreateMaintenanceRequest {
  equipmentId: string;
  incidentId?: string;
  technicianId: string;
  type: MaintenanceType;
  reportedProblem: string;
  laborCost: number;
  otherCosts: number;
  nextMaintenanceDate?: string;
  observations?: string;
  startedAt?: string;
}

export interface UpdateMaintenanceRequest {
  status?: MaintenanceStatus;
  reportedProblem?: string;
  workDone?: string;
  laborCost?: number;
  otherCosts?: number;
  nextMaintenanceDate?: string;
  observations?: string;
  technicianId?: string;
}

export interface CloseMaintenanceRequest {
  workDone: string;
  laborCost?: number;
  otherCosts?: number;
  nextMaintenanceDate?: string;
  observations?: string;
  // Solo se aceptan AVAILABLE, IN_USE, OUT_OF_SERVICE o DECOMMISSIONED
  equipmentFinalStatus?: EquipmentStatus;
  // Solo RESOLVED o CLOSED; aplica si el mantenimiento tiene incidente
  incidentFinalStatus?: IncidentStatus;
}

export interface AddSparePartToMaintenanceRequest {
  sparePartId: string;
  quantity: number;
}

export interface EquipmentHistory {
  equipmentId: string;
  equipmentInternalCode?: string | null;
  totalLaborCost: number;
  totalSparePartsCost: number;
  totalOtherCosts: number;
  totalCost: number;
  maintenances: Maintenance[];
  incidents: Incident[];
}