import { api } from "./api";
import type {
  EquipmentComponent,
  CreateEquipmentComponentRequest,
  UpdateEquipmentComponentRequest,
} from "../types/equipmentComponent";

export async function getComponentsByEquipment(
  equipmentId: string
): Promise<EquipmentComponent[]> {
  const res = await api.get<EquipmentComponent[]>(
    `/equipmentcomponent/GetComponentsByEquipment/${equipmentId}`
  );
  return res.data;
}

export async function createComponent(
  data: CreateEquipmentComponentRequest
): Promise<EquipmentComponent> {
  const res = await api.post<EquipmentComponent>(
    "/equipmentcomponent/PostNewComponent",
    data
  );
  return res.data;
}

export async function updateComponent(
  id: string,
  data: UpdateEquipmentComponentRequest
): Promise<EquipmentComponent> {
  const res = await api.put<EquipmentComponent>(
    `/equipmentcomponent/PutComponent/${id}`,
    data
  );
  return res.data;
}

export async function deleteComponent(id: string): Promise<void> {
  await api.delete(`/equipmentcomponent/DeleteComponent/${id}`);
}
