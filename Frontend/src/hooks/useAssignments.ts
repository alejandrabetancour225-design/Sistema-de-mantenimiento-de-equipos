import { useQuery } from "@tanstack/react-query";
import { getAssignments } from "../services/assignmentServices";
import { getUsers } from "../services/usersServices";
import { useAuth } from "../context/authContext";
import type { AssignableUser } from "../types/assignment";

export function useAssignments(enabled = true) {
  return useQuery({
    queryKey: ["assignments"],
    queryFn: getAssignments,
    enabled,
  });
}

// Empleados a los que se puede asignar un equipo.
// - Administrador: sale de /users (solo él puede listarlos), filtrando Empleados activos.
// - Técnico: /users le da 403, así que se arma con las personas que ya tuvieron
//   alguna asignación (limitación mientras el backend no exponga una lista para el Técnico).
export function useAssignableUsers(enabled = true) {
  const { role } = useAuth();
  const isAdmin = role === "Administrador";

  const users = useQuery({
    queryKey: ["users"],
    queryFn: getUsers,
    enabled: enabled && isAdmin,
  });
  const assignments = useAssignments(enabled && !isAdmin);

  let data: AssignableUser[] | undefined;
  if (isAdmin) {
    data = users.data
      ?.filter((u) => u.active && u.role === "Empleado")
      .map((u) => ({ id: u.id, fullName: u.fullName }));
  } else if (assignments.data) {
    const seen = new Map<string, AssignableUser>();
    for (const a of assignments.data) {
      if (!seen.has(a.userId)) {
        seen.set(a.userId, { id: a.userId, fullName: a.userFullName ?? "Sin nombre" });
      }
    }
    data = [...seen.values()].sort((a, b) => a.fullName.localeCompare(b.fullName));
  }

  return { data, isLoading: users.isLoading || assignments.isLoading };
}
