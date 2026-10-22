export type RiskLevel = "LOW" | "MEDIUM" | "HIGH";

export interface FailurePrediction {
  id: string;
  equipmentId: string;
  equipmentInternalCode: string;
  equipmentBrand: string;
  equipmentModel: string;
  riskScore: number;
  riskLevel: RiskLevel;
  riskComponent: string;
  reason: string;
  modelVersion: string;
  acknowledged: boolean;
  predictedAt: string;
}
