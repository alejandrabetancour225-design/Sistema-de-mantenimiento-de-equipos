import { api } from "./api";
import type {
  DashboardEquipmentSummary,
  DashboardOpenIncident,
  DashboardUpcomingMaintenance,
} from "../types/dashboard";

export async function getEquipmentSummary(): Promise<DashboardEquipmentSummary> {
  const res = await api.get<DashboardEquipmentSummary>("/dashboard/GetEquipmentSummary");
  return res.data;
}

export async function getOpenIncidents(): Promise<DashboardOpenIncident[]> {
  const res = await api.get<DashboardOpenIncident[]>("/dashboard/GetOpenIncidents");
  return res.data;
}

export async function getUpcomingMaintenances(daysAhead = 30): Promise<DashboardUpcomingMaintenance[]> {
  const res = await api.get<DashboardUpcomingMaintenance[]>(
    `/dashboard/GetUpcomingMaintenances?daysAhead=${daysAhead}`
  );
  return res.data;
}
