import { useQuery } from "@tanstack/react-query";
import { getAssignableUsers, getAssignments } from "../services/assignmentServices";

export function useAssignments(enabled = true) {
  return useQuery({
    queryKey: ["assignments"],
    queryFn: getAssignments,
    enabled,
  });
}

// Personas a las que se puede asignar un equipo (GET /api/assignment/GetAssignableUsers).
export function useAssignableUsers(enabled = true) {
  // Personal interno activo (Empleado, Técnico o Administrador); mismo criterio que la API.
  const users = useQuery({
    queryKey: ["assignable-users"],
    queryFn: getAssignableUsers,
    enabled,
  });
  return { data: users.data, isLoading: users.isLoading };
}
