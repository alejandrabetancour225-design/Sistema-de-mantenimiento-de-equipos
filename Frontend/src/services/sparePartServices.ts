import { api } from "./api";
import type {
  SparePart,
  CreateSparePartRequest,
  UpdateSparePartRequest,
} from "../types/sparePart";

export async function getSpareParts(includeInactive = false): Promise<SparePart[]> {
  const res = await api.get<SparePart[]>("/sparepart/GetSparePartList", {
    params: { includeInactive },
  });
  return res.data;
}

export async function createSparePart(data: CreateSparePartRequest): Promise<SparePart> {
  const res = await api.post<SparePart>("/sparepart/PostNewSparePart", data);
  return res.data;
}

export async function updateSparePart(
  id: string,
  data: UpdateSparePartRequest
): Promise<SparePart> {
  const res = await api.put<SparePart>(`/sparepart/PutSparePart/${id}`, data);
  return res.data;
}

export async function deactivateSparePart(id: string): Promise<void> {
  await api.patch(`/sparepart/DeactivateSparePart/${id}`);
}

export async function reactivateSparePart(id: string): Promise<void> {
  await api.patch(`/sparepart/ReactivateSparePart/${id}`);
}
