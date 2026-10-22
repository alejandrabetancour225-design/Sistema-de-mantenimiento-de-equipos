export interface RankedCause {
  cause: string;
  probability: number;
  recommendation: string;
}

export interface DiagnosisContextSnapshot {
  brand: string;
  model: string;
  osInfo: string;
  components: string[];
}

export interface AIDiagnosis {
  id: string;
  equipmentId: string;
  equipmentInternalCode: string;
  incidentId: string | null;
  createdByUserId: string;
  createdByName: string;
  symptom: string;
  rankedCauses: RankedCause[];
  requiresTechnician: boolean;
  reasoning: string;
  modelVersion: string;
  confidence: number;
  status: string;
  createdAt: string;
  contextSnapshot: DiagnosisContextSnapshot;
}

export interface CreateDiagnosisRequest {
  equipmentId: string;
  symptom: string;
  incidentId?: string;
}
