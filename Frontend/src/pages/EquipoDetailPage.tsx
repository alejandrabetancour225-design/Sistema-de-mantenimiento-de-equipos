import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { getEquipmentById } from "../services/equipmentServices";
import { useEquipmentComponents, useEquipmentIncidents } from "../hooks/useEquipmentDetail";
import { createComponent, deleteComponent } from "../services/equipmentComponentServices";
import { createIncident, updateIncident, deleteIncident } from "../services/incidentServices";
import { EQUIPMENT_STATUS_LABEL } from "../types/equipment";
import { INCIDENT_STATUS_LABEL, type IncidentStatus } from "../types/incident";
import { useAuth } from "../context/authContext";
import MaintenanceSection from "../components/MaintenanceSection";

export default function EquipoDetailPage() {
  const { id } = useParams<{ id: string }>();
  const equipmentId = id!;
  const { role } = useAuth();
  const canManageComponents = role === "Administrador" || role === "Técnico";
  const canManageIncidents = role === "Administrador" || role === "Empleado";
  const queryClient = useQueryClient();

  const { data: equipment, isLoading: loadingEquipment } = useQuery({
    queryKey: ["equipment", equipmentId],
    queryFn: () => getEquipmentById(equipmentId),
  });
  const { data: components, isLoading: loadingComponents } = useEquipmentComponents(equipmentId);
  const { data: incidents, isLoading: loadingIncidents } = useEquipmentIncidents(equipmentId);

  // --- Formulario de componente ---
  const [compForm, setCompForm] = useState({
    componentType: "",
    brand: "",
    model: "",
    serialNumber: "",
  });
  const createCompMutation = useMutation({
    mutationFn: createComponent,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment-components", equipmentId] });
      setCompForm({ componentType: "", brand: "", model: "", serialNumber: "" });
    },
  });
  const deleteCompMutation = useMutation({
    mutationFn: deleteComponent,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["equipment-components", equipmentId] }),
  });

  function handleCreateComponent(e: React.FormEvent) {
    e.preventDefault();
    createCompMutation.mutate({ equipmentId, ...compForm });
  }

  // --- Formulario de incidente ---
  const [incidentDescription, setIncidentDescription] = useState("");
  const createIncidentMutation = useMutation({
    mutationFn: createIncident,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment-incidents", equipmentId] });
      setIncidentDescription("");
    },
  });
  const updateIncidentMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: IncidentStatus }) =>
      updateIncident(id, { status }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["equipment-incidents", equipmentId] }),
  });
  const deleteIncidentMutation = useMutation({
    mutationFn: deleteIncident,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["equipment-incidents", equipmentId] }),
  });

  function handleCreateIncident(e: React.FormEvent) {
    e.preventDefault();
    if (!incidentDescription.trim()) return;
    createIncidentMutation.mutate({ equipmentId, description: incidentDescription });
  }

  const incidentError = (createIncidentMutation.error as any)?.response?.data?.message;

  if (loadingEquipment) return <p className="p-8">Cargando equipo...</p>;
  if (!equipment) return <p className="p-8 text-red-600">Equipo no encontrado.</p>;

  return (
    <div className="p-8 space-y-8 max-w-3xl">
      <div>
        <Link to="/equipos" className="text-sm text-blue-600">
          ← Volver a equipos
        </Link>
        <h1 className="text-xl font-semibold mt-2">
          {equipment.internalCode} — {equipment.brand} {equipment.model}
        </h1>
        <p className="text-sm text-gray-600">
          {equipment.type} · {equipment.location} · {EQUIPMENT_STATUS_LABEL[equipment.status]}
        </p>
      </div>

      {/* Componentes */}
      <section className="space-y-3">
        <h2 className="font-medium">Componentes</h2>

        {canManageComponents && (
          <form onSubmit={handleCreateComponent} className="flex flex-wrap gap-2 text-sm">
            <input
              placeholder="Tipo (RAM, disco...)"
              value={compForm.componentType}
              onChange={(e) => setCompForm({ ...compForm, componentType: e.target.value })}
              className="input flex-1 min-w-[120px]"
            />
            <input
              placeholder="Marca"
              value={compForm.brand}
              onChange={(e) => setCompForm({ ...compForm, brand: e.target.value })}
              className="input flex-1 min-w-[100px]"
            />
            <input
              placeholder="Modelo"
              value={compForm.model}
              onChange={(e) => setCompForm({ ...compForm, model: e.target.value })}
              className="input flex-1 min-w-[100px]"
            />
            <input
              placeholder="Serie"
              value={compForm.serialNumber}
              onChange={(e) => setCompForm({ ...compForm, serialNumber: e.target.value })}
              className="input flex-1 min-w-[100px]"
            />
            <button
              type="submit"
              disabled={createCompMutation.isPending}
              className="bg-blue-600 text-white px-3 py-2 rounded disabled:opacity-50"
            >
              Agregar
            </button>
          </form>
        )}

        {loadingComponents ? (
          <p className="text-sm text-gray-500">Cargando...</p>
        ) : (
          <ul className="divide-y border rounded text-sm">
            {components?.map((c) => (
              <li key={c.id} className="flex justify-between items-center px-3 py-2">
                <span>
                  {c.componentType} — {c.brand} {c.model} ({c.serialNumber})
                </span>
                {canManageComponents && (
                  <button
                    onClick={() => deleteCompMutation.mutate(c.id)}
                    className="text-red-600 text-xs"
                  >
                    Eliminar
                  </button>
                )}
              </li>
            ))}
            {components?.length === 0 && (
              <li className="px-3 py-2 text-gray-500">Sin componentes registrados.</li>
            )}
          </ul>
        )}
      </section>

      {/* Incidentes */}
      <section className="space-y-3">
        <h2 className="font-medium">Incidentes</h2>

        {canManageIncidents && (
          <form onSubmit={handleCreateIncident} className="flex gap-2 text-sm">
            <input
              placeholder="Describe el problema..."
              value={incidentDescription}
              onChange={(e) => setIncidentDescription(e.target.value)}
              className="input flex-1"
            />
            <button
              type="submit"
              disabled={createIncidentMutation.isPending}
              className="bg-blue-600 text-white px-3 py-2 rounded disabled:opacity-50"
            >
              Reportar
            </button>
          </form>
        )}
        {incidentError && <p className="text-sm text-red-600">{incidentError}</p>}

        {loadingIncidents ? (
          <p className="text-sm text-gray-500">Cargando...</p>
        ) : (
          <ul className="divide-y border rounded text-sm">
            {incidents?.map((inc) => (
              <li key={inc.id} className="flex justify-between items-center px-3 py-2 gap-2">
                <div>
                  <p>{inc.description}</p>
                  <p className="text-xs text-gray-500">
                    Reportado por {inc.reportedByName} ·{" "}
                    {new Date(inc.reportedAt).toLocaleDateString()}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  {canManageIncidents ? (
                    <select
                      value={inc.status}
                      onChange={(e) =>
                        updateIncidentMutation.mutate({
                          id: inc.id,
                          status: e.target.value as IncidentStatus,
                        })
                      }
                      className="rounded border px-2 py-1 text-xs"
                    >
                      {Object.entries(INCIDENT_STATUS_LABEL).map(([value, label]) => (
                        <option key={value} value={value}>
                          {label}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <span className="text-xs">{INCIDENT_STATUS_LABEL[inc.status]}</span>
                  )}
                  {canManageIncidents && (
                    <button
                      onClick={() => deleteIncidentMutation.mutate(inc.id)}
                      className="text-red-600 text-xs"
                    >
                      Eliminar
                    </button>
                  )}
                </div>
              </li>
            ))}
            {incidents?.length === 0 && (
              <li className="px-3 py-2 text-gray-500">Sin incidentes registrados.</li>
            )}
          </ul>
        )}
      </section>

      {/* Mantenimientos (nuevo) */}
      {canManageComponents && <MaintenanceSection equipmentId={equipmentId} />}
    </div>
  );
}