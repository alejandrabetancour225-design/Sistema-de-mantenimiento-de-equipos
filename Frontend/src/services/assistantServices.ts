import { api } from "./api";

export type ChatRole = "empleado" | "asistente";

export interface ChatMessage {
  role: ChatRole;
  content: string;
}

export interface TriageResponse {
  understanding: string;
  needsMoreInfo: boolean;
  question: string | null;
  questionsRemaining: number;
  safeTips: string[];
  technicalSummary: string;
  urgency: "baja" | "media" | "alta";
  requiresTechnician: boolean;
  detectedExpressions: string[];
}

// La IA en CPU puede tardar hasta ~3 minutos.
export async function triage(equipmentId: string, messages: ChatMessage[]): Promise<TriageResponse> {
  const res = await api.post<TriageResponse>(
    "/assistant/Triage",
    { equipmentId, messages },
    { timeout: 240_000 }
  );
  return res.data;
}

export interface GlossaryTerm {
  id: string;
  expression: string;
  meaning: string;
  category: string;
  isBase: boolean;
  active: boolean;
}

export interface GlossaryTermInput {
  expression: string;
  meaning: string;
  category?: string;
  active: boolean;
}

export async function getGlossary(): Promise<GlossaryTerm[]> {
  return (await api.get<GlossaryTerm[]>("/glossary")).data;
}

export async function createGlossaryTerm(data: GlossaryTermInput): Promise<GlossaryTerm> {
  return (await api.post<GlossaryTerm>("/glossary", data)).data;
}

export async function updateGlossaryTerm(id: string, data: GlossaryTermInput): Promise<GlossaryTerm> {
  return (await api.put<GlossaryTerm>(`/glossary/${id}`, data)).data;
}

export async function deleteGlossaryTerm(id: string): Promise<void> {
  await api.delete(`/glossary/${id}`);
}
