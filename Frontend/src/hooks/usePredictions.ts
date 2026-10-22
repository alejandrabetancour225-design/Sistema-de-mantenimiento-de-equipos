import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acknowledgePrediction,
  getHighRiskPredictions,
  getPredictionByEquipment,
  refreshAllPredictions,
  createPreventiveMaintenance,
} from "../services/predictionServices";

export function useHighRiskPredictions(take = 10) {
  return useQuery({
    queryKey: ["predictions", "high-risk", take],
    queryFn: () => getHighRiskPredictions(take),
  });
}

export function usePredictionByEquipment(equipmentId: string | undefined) {
  return useQuery({
    queryKey: ["predictions", "equipment", equipmentId],
    queryFn: () => getPredictionByEquipment(equipmentId!),
    enabled: !!equipmentId,
  });
}

export function useRefreshPredictions() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: refreshAllPredictions,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}

export function useAcknowledgePrediction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: acknowledgePrediction,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
    },
  });
}

export function useCreatePreventiveMaintenance() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ equipmentId, reportedProblem }: { equipmentId: string; reportedProblem: string }) =>
      createPreventiveMaintenance(equipmentId, reportedProblem),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["predictions"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      queryClient.invalidateQueries({ queryKey: ["maintenances"] });
    },
  });
}
