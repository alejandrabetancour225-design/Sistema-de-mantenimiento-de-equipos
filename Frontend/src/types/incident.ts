export type IncidentStatus = "OPEN" | "IN_PROGRESS" | "CLOSED" | "RESOLVED";

export const INCIDENT_STATUS_LABEL: Record<IncidentStatus, string> = {
  OPEN: "Abierto",
  IN_PROGRESS: "En progreso",
  CLOSED: "Cerrado",
  RESOLVED: "Resuelto",
};

export interface Incident {
  id: string;
  equipmentId: string;
  equipmentInternalCode?: string | null;
  reportedBy: string;
  reportedByName?: string | null;
  description: string;
  reportedAt: string;
  status: IncidentStatus;
  maintenanceId?: string | null;
}

export interface CreateIncidentRequest {
  equipmentId: string;
  description: string;
  reportedAt?: string;
  maintenanceId?: string;
}

export interface UpdateIncidentRequest {
  description?: string;
  status?: IncidentStatus;
  maintenanceId?: string;
}
