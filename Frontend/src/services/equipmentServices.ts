import { api } from "./api";
import type {
  CreateEquipmentRequest,
  Equipment,
  EquipmentStatus,
  UpdateEquipmentRequest,
} from "../types/equipment";

export async function getEquipmentList(): Promise<Equipment[]> {
  const res = await api.get<Equipment[]>("/equipment/GetEquipmentList");
  return res.data;
}

export async function getEquipmentById(id: string): Promise<Equipment> {
  const res = await api.get<Equipment>(`/equipment/GetEquipmentById/${id}`);
  return res.data;
}

export async function createEquipment(data: CreateEquipmentRequest): Promise<Equipment> {
  const res = await api.post<Equipment>("/equipment/PostNewEquipment", data);
  return res.data;
}

export async function updateEquipment(
  id: string,
  data: UpdateEquipmentRequest
): Promise<Equipment> {
  const res = await api.put<Equipment>(`/equipment/PutEquipment/${id}`, data);
  return res.data;
}

export async function changeEquipmentStatus(
  id: string,
  status: EquipmentStatus
): Promise<Equipment> {
  const res = await api.patch<Equipment>(`/equipment/PatchEquipmentStatus/${id}`, { status });
  return res.data;
}

export async function deleteEquipment(id: string): Promise<void> {
  await api.delete(`/equipment/DeleteEquipment/${id}`);
}