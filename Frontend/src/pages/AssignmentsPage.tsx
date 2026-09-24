import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useAssignments } from "../hooks/useAssignments";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { useUsers } from "../hooks/useUsers";
import { assignEquipment, releaseAssignment } from "../services/assignmentServices";
import { useAuth } from "../context/authContext";

export default function AssignmentsPage() {
  const { role } = useAuth();
  const canManage = role === "Administrador" || role === "Técnico";

  const { data: assignments, isLoading, error } = useAssignments();
  const { data: equipment } = useEquipmentList();
  const { data: users } = useUsers();
  const queryClient = useQueryClient();

  const [equipmentId, setEquipmentId] = useState("");
  const [userId, setUserId] = useState("");
  const [observations, setObservations] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const assignMutation = useMutation({
    mutationFn: assignEquipment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["assignments"] });
      setEquipmentId("");
      setUserId("");
      setObservations("");
      setFormError(null);
    },
    onError: (err: any) => {
      setFormError(err.response?.data?.message ?? "No se pudo asignar el equipo.");
    },
  });

  const releaseMutation = useMutation({
    mutationFn: (id: string) => releaseAssignment(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["assignments"] }),
  });

  function handleAssign(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    if (!equipmentId || !userId) {
      setFormError("Selecciona un equipo y un usuario.");
      return;
    }
    assignMutation.mutate({ equipmentId, userId, observations: observations || undefined });
  }

  // Solo equipos sin asignación activa tienen sentido para asignar
  const assignedEquipmentIds = new Set(
    (assignments ?? []).filter((a) => a.status === "ACTIVE").map((a) => a.equipmentId)
  );
  const availableEquipment = (equipment ?? []).filter((eq) => !assignedEquipmentIds.has(eq.id));

  if (isLoading) return <p className="p-8">Cargando asignaciones...</p>;
  if (error) return <p className="p-8 text-red-600">Error al cargar las asignaciones.</p>;

  return (
    <div className="p-8 space-y-6">
      <h1 className="text-xl font-semibold">
        {canManage ? "Asignaciones" : "Mis equipos asignados"}
      </h1>

      {canManage && (
        <form onSubmit={handleAssign} className="space-y-3 max-w-lg border rounded p-4">
          <h2 className="font-medium">Asignar equipo</h2>

          <div>
            <label className="block text-sm font-medium">Equipo</label>
            <select
              value={equipmentId}
              onChange={(e) => setEquipmentId(e.target.value)}
              className="input"
            >
              <option value="">Selecciona un equipo disponible</option>
              {availableEquipment.map((eq) => (
                <option key={eq.id} value={eq.id}>
                  {eq.internalCode} — {eq.brand} {eq.model}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium">Usuario</label>
            <select
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              className="input"
            >
              <option value="">Selecciona un usuario</option>
              {(users ?? []).map((u) => (
                <option key={u.id} value={u.id}>
                  {u.fullName} ({u.role})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium">Observaciones (opcional)</label>
            <input
              value={observations}
              onChange={(e) => setObservations(e.target.value)}
              className="input"
            />
          </div>

          {formError && <p className="text-sm text-red-600">{formError}</p>}

          <button
            type="submit"
            disabled={assignMutation.isPending}
            className="bg-blue-600 text-white px-4 py-2 rounded disabled:opacity-50"
          >
            {assignMutation.isPending ? "Asignando..." : "Asignar"}
          </button>
        </form>
      )}

      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2">Equipo</th>
            {canManage && <th>Usuario</th>}
            <th>Asignado</th>
            <th>Liberado</th>
            <th>Estado</th>
            <th>Observaciones</th>
            {canManage && <th></th>}
          </tr>
        </thead>
        <tbody>
          {assignments?.map((a) => (
            <tr key={a.id} className="border-b">
              <td className="py-2">
                {a.equipmentInternalCode} ({a.equipmentSerialNumber})
              </td>
              {canManage && <td>{a.userFullName}</td>}
              <td>{new Date(a.assignedAt).toLocaleDateString()}</td>
              <td>{a.releasedAt ? new Date(a.releasedAt).toLocaleDateString() : "—"}</td>
              <td>
                <span
                  className={`rounded-full px-2 py-1 text-xs font-medium ${
                    a.status === "ACTIVE"
                      ? "bg-blue-100 text-blue-800"
                      : "bg-gray-200 text-gray-600"
                  }`}
                >
                  {a.status === "ACTIVE" ? "Activa" : "Liberada"}
                </span>
              </td>
              <td>{a.observations || "—"}</td>
              {canManage && (
                <td>
                  {a.status === "ACTIVE" && (
                    <button
                      onClick={() => releaseMutation.mutate(a.id)}
                      disabled={releaseMutation.isPending}
                      className="text-red-600"
                    >
                      Liberar
                    </button>
                  )}
                </td>
              )}
            </tr>
          ))}
          {assignments?.length === 0 && (
            <tr>
              <td colSpan={canManage ? 7 : 5} className="py-4 text-center text-gray-500">
                No hay asignaciones {canManage ? "" : "para tu usuario"}.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
