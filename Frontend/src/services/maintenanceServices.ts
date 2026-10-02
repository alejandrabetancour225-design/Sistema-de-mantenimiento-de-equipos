import { api } from "./api";
import type {
  Maintenance,
  CreateMaintenanceRequest,
  UpdateMaintenanceRequest,
  CloseMaintenanceRequest,
  AddSparePartToMaintenanceRequest,
  EquipmentHistory,
} from "../types/maintenance";

export async function getMaintenancesByEquipment(
  equipmentId: string
): Promise<Maintenance[]> {
  const res = await api.get<Maintenance[]>(
    `/maintenance/GetMaintenancesByEquipment/${equipmentId}`
  );
  return res.data;
}

export async function getEquipmentHistory(equipmentId: string): Promise<EquipmentHistory> {
  const res = await api.get<EquipmentHistory>(
    `/maintenance/GetHistoryByEquipment/${equipmentId}`
  );
  return res.data;
}

export async function createMaintenance(
  data: CreateMaintenanceRequest
): Promise<Maintenance> {
  const res = await api.post<Maintenance>("/maintenance/PostNewMaintenance", data);
  return res.data;
}

export async function createMaintenanceFromIncident(
  incidentId: string
): Promise<Maintenance> {
  const res = await api.post<Maintenance>(`/maintenance/CreateFromIncident/${incidentId}`);
  return res.data;
}

export async function updateMaintenance(
  id: string,
  data: UpdateMaintenanceRequest
): Promise<Maintenance> {
  const res = await api.put<Maintenance>(`/maintenance/PutMaintenance/${id}`, data);
  return res.data;
}

export async function closeMaintenance(
  id: string,
  data: CloseMaintenanceRequest
): Promise<Maintenance> {
  const res = await api.post<Maintenance>(`/maintenance/CloseMaintenance/${id}`, data);
  return res.data;
}

export async function addSparePart(
  maintenanceId: string,
  data: AddSparePartToMaintenanceRequest
): Promise<Maintenance> {
  const res = await api.post<Maintenance>(
    `/maintenance/AddSparePart/${maintenanceId}`,
    data
  );
  return res.data;
}

export async function removeSparePart(
  maintenanceId: string,
  maintenanceSparePartId: string
): Promise<void> {
  await api.delete(
    `/maintenance/RemoveSparePart/${maintenanceId}/${maintenanceSparePartId}`
  );
}

export async function deleteMaintenance(id: string): Promise<void> {
  await api.delete(`/maintenance/DeleteMaintenance/${id}`);
}