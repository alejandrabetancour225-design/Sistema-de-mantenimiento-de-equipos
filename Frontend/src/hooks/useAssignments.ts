import { useQuery } from "@tanstack/react-query";
import { getAssignments } from "../services/assignmentServices";

export function useAssignments() {
  return useQuery({
    queryKey: ["assignments"],
    queryFn: getAssignments,
  });
}