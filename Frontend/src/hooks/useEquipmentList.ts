import { useQuery } from "@tanstack/react-query";
import { getEquipmentList } from "../services/equipmentServices";

export function useEquipmentList() {
  return useQuery({
    queryKey: ["equipment"],
    queryFn: getEquipmentList,
  });
}