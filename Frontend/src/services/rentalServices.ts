import { api } from "./api";

export interface Lessor {
  id: string;
  name: string;
  taxId?: string | null;
  contactName: string;
  contactEmail: string;
  contactPhone?: string | null;
  active: boolean;
}

export interface LessorInput {
  name: string;
  taxId?: string;
  contactName: string;
  contactEmail: string;
  contactPhone?: string;
  active: boolean;
}

export interface RentalRate {
  equipmentType: string;
  monthlyRate: number;
}

export interface RentalContract {
  id: string;
  lessorId: string;
  lessorName: string;
  lessorEmail: string;
  contractNumber: string;
  startDate: string;
  endDate: string;
  daysToExpiry: number;
  status: "ACTIVE" | "ENDED" | "CANCELLED";
  allowsInternalPreventive: boolean;
  notes?: string | null;
  rates: RentalRate[];
  activeEquipment: number;
  returnedEquipment: number;
  monthlyCost: number;
}

export interface RentalContractInput {
  lessorId: string;
  contractNumber: string;
  startDate: string;
  endDate: string;
  allowsInternalPreventive: boolean;
  notes?: string;
  rates: RentalRate[];
}

export interface LessorClaim {
  id: string;
  equipmentId: string;
  equipmentCode: string;
  contractNumber: string;
  lessorName: string;
  description: string;
  status: "OPEN" | "ANSWERED" | "RESOLVED";
  lessorResponse?: string | null;
  notifiedAt?: string | null;
  createdAt: string;
  resolvedAt?: string | null;
}

export const CLAIM_STATUS_LABEL: Record<LessorClaim["status"], string> = {
  OPEN: "Abierto",
  ANSWERED: "Respondido",
  RESOLVED: "Resuelto",
};

export const getLessors = async () => (await api.get<Lessor[]>("/rental/Lessors")).data;
export const createLessor = async (d: LessorInput) => (await api.post<Lessor>("/rental/Lessors", d)).data;
export const updateLessor = async (id: string, d: LessorInput) => (await api.put<Lessor>(`/rental/Lessors/${id}`, d)).data;

export const getContracts = async () => (await api.get<RentalContract[]>("/rental/Contracts")).data;
export const getExpiringContracts = async (days = 30) =>
  (await api.get<RentalContract[]>(`/rental/Contracts/Expiring?days=${days}`)).data;
export const createContract = async (d: RentalContractInput) => (await api.post("/rental/Contracts", d)).data;
export const updateContract = async (id: string, d: RentalContractInput) => (await api.put(`/rental/Contracts/${id}`, d)).data;
export const returnWholeContract = async (id: string) =>
  (await api.post<{ returned: string[]; pending: string[]; message: string }>(`/rental/Contracts/${id}/ReturnAll`)).data;

export const returnEquipment = async (equipmentId: string) =>
  (await api.post(`/rental/Equipment/${equipmentId}/Return`)).data;
export const registerReplacement = async (
  equipmentId: string,
  d: { internalCode: string; serialNumber: string; brand: string; model: string; location?: string }
) => (await api.post<{ id: string; internalCode: string }>(`/rental/Equipment/${equipmentId}/Replacement`, d)).data;

export const getClaims = async () => (await api.get<LessorClaim[]>("/rental/Claims")).data;
export const createClaim = async (d: { equipmentId: string; description: string; incidentId?: string }) =>
  (await api.post<{ id: string; notifiedAt: string | null; lessorEmail: string }>("/rental/Claims", d)).data;
export const updateClaim = async (id: string, d: { status: LessorClaim["status"]; lessorResponse?: string }) =>
  (await api.put(`/rental/Claims/${id}`, d)).data;
