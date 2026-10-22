import { api } from "./api";
import type { FailurePrediction } from "../types/prediction";

export async function getHighRiskPredictions(take = 10): Promise<FailurePrediction[]> {
  const res = await api.get<FailurePrediction[]>(`/prediction/GetHighRisk?take=${take}`);
  return res.data;
}

export async function getPredictionByEquipment(equipmentId: string): Promise<FailurePrediction | null> {
  try {
    const res = await api.get<FailurePrediction>(`/prediction/GetByEquipment/${equipmentId}`);
    return res.data;
  } catch {
    return null;
  }
}

export async function refreshAllPredictions(): Promise<{ count: number }> {
  const res = await api.post<{ count: number }>("/prediction/RefreshAll");
  return res.data;
}

export async function acknowledgePrediction(predictionId: string): Promise<void> {
  await api.post(`/prediction/Acknowledge/${predictionId}`);
}

export async function createPreventiveMaintenance(
  equipmentId: string,
  reportedProblem: string
): Promise<{ id: string }> {
  const res = await api.post(`/maintenance/CreatePreventiveFromPrediction/${equipmentId}`, {
    reportedProblem,
  });
  return res.data;
}
