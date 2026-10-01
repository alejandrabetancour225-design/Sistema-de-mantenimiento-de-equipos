import { api } from "./api";
import type {
  Incident,
  CreateIncidentRequest,
  UpdateIncidentRequest,
} from "../types/incident";

export async function getIncidentsByEquipment(equipmentId: string): Promise<Incident[]> {
  const res = await api.get<Incident[]>(`/incident/GetIncidentsByEquipment/${equipmentId}`);
  return res.data;
}

export async function createIncident(data: CreateIncidentRequest): Promise<Incident> {
  const res = await api.post<Incident>("/incident/PostNewIncident", data);
  return res.data;
}

export async function updateIncident(
  id: string,
  data: UpdateIncidentRequest
): Promise<Incident> {
  const res = await api.put<Incident>(`/incident/PutIncident/${id}`, data);
  return res.data;
}

export async function deleteIncident(id: string): Promise<void> {
  await api.delete(`/incident/DeleteIncident/${id}`);
}