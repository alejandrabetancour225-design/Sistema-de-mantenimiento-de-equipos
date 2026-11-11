import { useQueryClient } from "@tanstack/react-query";

export const money = (n: number) => `$${n.toFixed(2)}`;

// Cualquier cambio de mantenimiento puede mover equipo, incidente y dashboard.
export function useInvalidateAll() {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ["maintenances"] });
    queryClient.invalidateQueries({ queryKey: ["equipment"] });
    queryClient.invalidateQueries({ queryKey: ["equipment-incidents"] });
    queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  };
}