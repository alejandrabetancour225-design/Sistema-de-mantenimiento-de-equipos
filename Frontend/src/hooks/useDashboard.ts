import { useQuery } from "@tanstack/react-query";
import {
  getEquipmentSummary,
  getOpenIncidents,
  getUpcomingMaintenances,
} from "../services/dashboardServices";

export function useEquipmentSummary() {
  return useQuery({
    queryKey: ["dashboard", "equipment-summary"],
    queryFn: getEquipmentSummary,
  });
}

export function useOpenIncidents() {
  return useQuery({
    queryKey: ["dashboard", "open-incidents"],
    queryFn: getOpenIncidents,
  });
}

export function useUpcomingMaintenances(daysAhead = 30) {
  return useQuery({
    queryKey: ["dashboard", "upcoming-maintenances", daysAhead],
    queryFn: () => getUpcomingMaintenances(daysAhead),
  });
}
