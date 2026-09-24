import { api } from "./api";
import type {
  Assignment,
  CreateAssignmentRequest,
  ReleaseAssignmentRequest,
} from "../types/assignment";

export async function getAssignments(): Promise<Assignment[]> {
  const res = await api.get<Assignment[]>("/assignment/GetAssignments");
  return res.data;
}

export async function assignEquipment(data: CreateAssignmentRequest): Promise<Assignment> {
  const res = await api.post<Assignment>("/assignment/AssignEquipment", data);
  return res.data;
}

export async function releaseAssignment(
  id: string,
  data?: ReleaseAssignmentRequest
): Promise<Assignment> {
  const res = await api.patch<Assignment>(`/assignment/ReleaseEquipment/${id}`, data ?? {});
  return res.data;
}
