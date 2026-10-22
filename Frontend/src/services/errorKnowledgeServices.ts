import { api } from "./api";
import type {
  ErrorKnowledge,
  CreateErrorKnowledgeRequest,
  UpdateErrorKnowledgeRequest,
} from "../types/errorKnowledge";

export async function getErrorKnowledgeList(params?: {
  component?: string;
  symptom?: string;
}): Promise<ErrorKnowledge[]> {
  const res = await api.get<ErrorKnowledge[]>("/errorknowledge/GetKnowledgeList", { params });
  return res.data;
}

export async function getErrorKnowledgeById(id: string): Promise<ErrorKnowledge> {
  const res = await api.get<ErrorKnowledge>(`/errorknowledge/GetKnowledgeById/${id}`);
  return res.data;
}

export async function createErrorKnowledge(
  data: CreateErrorKnowledgeRequest
): Promise<ErrorKnowledge> {
  const res = await api.post<ErrorKnowledge>("/errorknowledge/PostNewKnowledge", data);
  return res.data;
}

export async function updateErrorKnowledge(
  id: string,
  data: UpdateErrorKnowledgeRequest
): Promise<ErrorKnowledge> {
  const res = await api.put<ErrorKnowledge>(`/errorknowledge/PutKnowledge/${id}`, data);
  return res.data;
}

export async function deleteErrorKnowledge(id: string): Promise<void> {
  await api.delete(`/errorknowledge/DeleteKnowledge/${id}`);
}
