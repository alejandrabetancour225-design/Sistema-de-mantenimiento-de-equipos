import { useQuery } from "@tanstack/react-query";
import { getComponentsByEquipment } from "../services/equipmentComponentServices";
import { getIncidentsByEquipment } from "../services/incidentServices";

export function useEquipmentComponents(equipmentId: string) {
  return useQuery({
    queryKey: ["equipment-components", equipmentId],
    queryFn: () => getComponentsByEquipment(equipmentId),
    enabled: !!equipmentId,
  });
}

export function useEquipmentIncidents(equipmentId: string) {
  return useQuery({
    queryKey: ["equipment-incidents", equipmentId],
    queryFn: () => getIncidentsByEquipment(equipmentId),
    enabled: !!equipmentId,
  });
}
