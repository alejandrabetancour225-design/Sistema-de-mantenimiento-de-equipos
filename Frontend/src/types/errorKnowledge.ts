export type ErrorSeverity = "LOW" | "MEDIUM" | "HIGH" | "CRITICAL";
export type ErrorSource = "Manual" | "Auto";

export const SEVERITY_LABEL: Record<ErrorSeverity, string> = {
  LOW: "Baja",
  MEDIUM: "Media",
  HIGH: "Alta",
  CRITICAL: "Crítica",
};

export const SEVERITY_COLOR: Record<ErrorSeverity, string> = {
  LOW: "bg-green-100 text-green-800",
  MEDIUM: "bg-yellow-100 text-yellow-800",
  HIGH: "bg-orange-100 text-orange-800",
  CRITICAL: "bg-red-100 text-red-800",
};

export interface ErrorKnowledge {
  id: string;
  symptom: string;
  componentType: string;
  possibleCauses: string;
  suggestedSolution: string;
  severity: ErrorSeverity;
  source: ErrorSource;
  confidence: number;
  active: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateErrorKnowledgeRequest {
  symptom: string;
  componentType: string;
  possibleCauses: string;
  suggestedSolution: string;
  severity: ErrorSeverity;
}

export type UpdateErrorKnowledgeRequest = Partial<CreateErrorKnowledgeRequest> & {
  active?: boolean;
};
