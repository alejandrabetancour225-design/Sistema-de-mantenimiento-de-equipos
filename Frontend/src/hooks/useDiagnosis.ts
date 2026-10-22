import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createDiagnosis,
  getDiagnosesByEquipment,
  getRecentDiagnoses,
} from "../services/diagnosisServices";
import type { CreateDiagnosisRequest } from "../types/diagnosis";

export function useRecentDiagnoses(take = 20) {
  return useQuery({
    queryKey: ["diagnoses", "recent", take],
    queryFn: () => getRecentDiagnoses(take),
  });
}

export function useDiagnosesByEquipment(equipmentId: string | undefined) {
  return useQuery({
    queryKey: ["diagnoses", "equipment", equipmentId],
    queryFn: () => getDiagnosesByEquipment(equipmentId!),
    enabled: !!equipmentId,
  });
}

export function useCreateDiagnosis() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDiagnosisRequest) => createDiagnosis(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["diagnoses"] });
    },
  });
}
