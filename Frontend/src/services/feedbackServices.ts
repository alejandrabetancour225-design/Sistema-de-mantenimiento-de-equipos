import { api } from "./api";
import type { Feedback, CreateFeedbackRequest, AIMetrics } from "../types/feedback";

export async function createFeedback(data: CreateFeedbackRequest): Promise<Feedback> {
  const res = await api.post<Feedback>("/feedback/CreateFeedback", data);
  return res.data;
}

export async function getFeedbacksByDiagnosis(diagnosisId: string): Promise<Feedback[]> {
  const res = await api.get<Feedback[]>(`/feedback/GetByDiagnosis/${diagnosisId}`);
  return res.data;
}

export async function getRecentFeedbacks(take = 20): Promise<Feedback[]> {
  const res = await api.get<Feedback[]>(`/feedback/GetRecentFeedbacks?take=${take}`);
  return res.data;
}

export async function getAIMetrics(): Promise<AIMetrics> {
  const res = await api.get<AIMetrics>("/feedback/GetMetrics");
  return res.data;
}
