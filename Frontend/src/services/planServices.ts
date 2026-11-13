import { api } from "./api";

export interface MaintenancePlan {
  id: string;
  equipmentId: string;
  equipmentCode: string;
  equipmentName: string;
  technicianId?: string | null;
  technicianName?: string | null;
  description: string;
  intervalWeeks: number;
  nextDate: string;
  active: boolean;
  lastCompletedAt?: string | null;
  overdue: boolean;
}

export interface MaintenancePlanInput {
  equipmentId: string;
  technicianId?: string | null;
  description: string;
  intervalWeeks: number;
  nextDate: string;
  active: boolean;
}

export interface CalendarEvent {
  id: string;
  kind: "plan" | "open" | "done" | "contract";
  title: string;
  date: string;
  planId?: string;
  equipmentId?: string;
  maintenanceId?: string;
  technician?: string | null;
  editable: boolean;
  projected?: boolean;
  overdue?: boolean;
}

export const getPlans = async (equipmentId?: string) =>
  (await api.get<MaintenancePlan[]>("/maintenanceplans", { params: { equipmentId } })).data;
export const getPlanTechnicians = async () =>
  (await api.get<{ id: string; fullName: string; role: string }[]>("/maintenanceplans/Technicians")).data;
export const createPlan = async (d: MaintenancePlanInput) => (await api.post("/maintenanceplans", d)).data;
export const updatePlan = async (id: string, d: MaintenancePlanInput) => (await api.put(`/maintenanceplans/${id}`, d)).data;
export const reschedulePlan = async (id: string, nextDate: string) =>
  (await api.patch(`/maintenanceplans/${id}/Reschedule`, { nextDate })).data;
export const startPlan = async (id: string) => (await api.post(`/maintenanceplans/${id}/Start`)).data;
export const getCalendar = async (from: string, to: string) =>
  (await api.get<CalendarEvent[]>("/calendar", { params: { from, to } })).data;
