export type EquipmentStatus =
  | "AVAILABLE"
  | "IN_USE"
  | "UNDER_MAINTENANCE"
  | "OUT_OF_SERVICE"
  | "DECOMMISSIONED";

export const EQUIPMENT_STATUS_LABEL: Record<EquipmentStatus, string> = {
  AVAILABLE: "Disponible",
  IN_USE: "En uso",
  UNDER_MAINTENANCE: "En mantenimiento",
  OUT_OF_SERVICE: "Fuera de servicio",
  DECOMMISSIONED: "Dado de baja",
};

export interface Equipment {
  id: string;
  internalCode: string;
  serialNumber: string;
  type: string;
  brand: string;
  model: string;
  characteristics?: Record<string, unknown> | null;
  acquisitionDate?: string | null;
  acquisitionPrice?: number | null;
  warrantyUntil?: string | null;
  location: string;
  status: EquipmentStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEquipmentRequest {
  internalCode: string;
  serialNumber: string;
  type: string;
  brand: string;
  model: string;
  characteristics?: Record<string, unknown>;
  acquisitionDate?: string;
  acquisitionPrice?: number;
  warrantyUntil?: string;
  location: string;
  status?: EquipmentStatus;
}

export type UpdateEquipmentRequest = Partial<CreateEquipmentRequest>;