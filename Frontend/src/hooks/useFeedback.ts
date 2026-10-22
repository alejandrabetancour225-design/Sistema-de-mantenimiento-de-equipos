import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createFeedback,
  getAIMetrics,
  getFeedbacksByDiagnosis,
  getRecentFeedbacks,
} from "../services/feedbackServices";
import type { CreateFeedbackRequest } from "../types/feedback";

export function useFeedbacksByDiagnosis(diagnosisId: string | undefined) {
  return useQuery({
    queryKey: ["feedbacks", "diagnosis", diagnosisId],
    queryFn: () => getFeedbacksByDiagnosis(diagnosisId!),
    enabled: !!diagnosisId,
  });
}

export function useRecentFeedbacks(take = 20) {
  return useQuery({
    queryKey: ["feedbacks", "recent", take],
    queryFn: () => getRecentFeedbacks(take),
  });
}

export function useAIMetrics() {
  return useQuery({
    queryKey: ["ai", "metrics"],
    queryFn: getAIMetrics,
  });
}

export function useCreateFeedback() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateFeedbackRequest) => createFeedback(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["feedbacks"] });
      queryClient.invalidateQueries({ queryKey: ["diagnoses"] });
      queryClient.invalidateQueries({ queryKey: ["ai", "metrics"] });
    },
  });
}
