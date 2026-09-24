export type AssignmentStatus = "ACTIVE" | "RELEASED";

export interface Assignment {
  id: string;
  equipmentId: string;
  userId: string;
  equipmentInternalCode?: string | null;
  equipmentSerialNumber?: string | null;
  userFullName?: string | null;
  assignedAt: string;
  releasedAt?: string | null;
  status: AssignmentStatus;
  observations: string;
}

export interface CreateAssignmentRequest {
  equipmentId: string;
  userId: string;
  observations?: string;
}

export interface ReleaseAssignmentRequest {
  observations?: string;
}