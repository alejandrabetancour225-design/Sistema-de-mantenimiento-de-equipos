import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createErrorKnowledge,
  deleteErrorKnowledge,
  getErrorKnowledgeById,
  getErrorKnowledgeList,
  updateErrorKnowledge,
} from "../services/errorKnowledgeServices";
import type {
  CreateErrorKnowledgeRequest,
  UpdateErrorKnowledgeRequest,
} from "../types/errorKnowledge";

export function useErrorKnowledgeList(params?: { component?: string; symptom?: string }) {
  return useQuery({
    queryKey: ["error-knowledge", params],
    queryFn: () => getErrorKnowledgeList(params),
  });
}

export function useErrorKnowledge(id: string | undefined) {
  return useQuery({
    queryKey: ["error-knowledge", "detail", id],
    queryFn: () => getErrorKnowledgeById(id!),
    enabled: !!id,
  });
}

export function useCreateErrorKnowledge() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateErrorKnowledgeRequest) => createErrorKnowledge(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["error-knowledge"] }),
  });
}

export function useUpdateErrorKnowledge() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateErrorKnowledgeRequest }) =>
      updateErrorKnowledge(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["error-knowledge"] }),
  });
}

export function useDeleteErrorKnowledge() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: deleteErrorKnowledge,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["error-knowledge"] }),
  });
}
