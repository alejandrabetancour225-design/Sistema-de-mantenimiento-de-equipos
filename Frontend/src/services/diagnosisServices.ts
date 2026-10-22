import { api } from "./api";
import type { AIDiagnosis, CreateDiagnosisRequest } from "../types/diagnosis";

export async function createDiagnosis(data: CreateDiagnosisRequest): Promise<AIDiagnosis> {
  const res = await api.post<AIDiagnosis>("/diagnosis/Diagnose", data);
  return res.data;
}

export async function getDiagnosisById(id: string): Promise<AIDiagnosis> {
  const res = await api.get<AIDiagnosis>(`/diagnosis/GetDiagnosisById/${id}`);
  return res.data;
}

export async function getDiagnosesByEquipment(equipmentId: string): Promise<AIDiagnosis[]> {
  const res = await api.get<AIDiagnosis[]>(
    `/diagnosis/GetDiagnosesByEquipment/${equipmentId}`
  );
  return res.data;
}

export async function getRecentDiagnoses(take = 20): Promise<AIDiagnosis[]> {
  const res = await api.get<AIDiagnosis[]>(
    `/diagnosis/GetRecentDiagnoses?take=${take}`
  );
  return res.data;
}
