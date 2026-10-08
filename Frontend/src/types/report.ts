export type ReportFormat = "pdf" | "excel";

export interface ReportFilters {
  from?: string;
  to?: string;
  type?: string;
  status?: string;
  assignedTo?: string;
  equipmentId?: string;
  technicianId?: string;
}
