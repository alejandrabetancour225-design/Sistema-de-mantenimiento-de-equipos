export interface DashboardEquipmentSummary {
  available: number;
  inUse: number;
  underMaintenance: number;
  outOfService: number;
  decommissioned: number;
  total: number;
}

export interface DashboardOpenIncident {
  id: string;
  equipmentInternalCode: string;
  description: string;
  reportedAt: string;
  status: string;
  daysOpen: number;
  isUrgent: boolean;
}

export interface DashboardUpcomingMaintenance {
  id: string;
  equipmentInternalCode: string;
  nextMaintenanceDate: string | null;
  daysUntil: number;
  isOverdue: boolean;
  technicianName: string;
}
