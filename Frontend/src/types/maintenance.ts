export type MaintenanceType = "PREVENTIVE" | "CORRECTIVE";
export type MaintenanceStatus = "OPEN" | "IN_PROGRESS" | "COMPLETED" | "CANCELLED";

export const MAINTENANCE_TYPE_LABEL: Record<MaintenanceType, string> = {
  PREVENTIVE: "Preventivo",
  CORRECTIVE: "Correctivo",
};

export const MAINTENANCE_STATUS_LABEL: Record<MaintenanceStatus, string> = {
  OPEN: "Abierto",
  IN_PROGRESS: "En progreso",
  COMPLETED: "Completado",
  CANCELLED: "Cancelado",
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
}