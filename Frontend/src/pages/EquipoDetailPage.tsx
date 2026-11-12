import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { changeEquipmentStatus, deleteEquipment, getEquipmentById } from "../services/equipmentServices";
import RentedEquipmentPanel from "../components/RentedEquipmentPanel";
import MaintenancePlansPanel from "../components/MaintenancePlansPanel";
import { manualStatusActions, statusLockedReason, type StatusAction } from "../lib/equipmentTransitions";
import { useAssignments } from "../hooks/useAssignments";
import { useEquipmentComponents, useEquipmentIncidents } from "../hooks/useEquipmentDetail";
import { createComponent, deleteComponent } from "../services/equipmentComponentServices";
import { createIncident, updateIncident, deleteIncident } from "../services/incidentServices";
import { EQUIPMENT_STATUS_LABEL, type EquipmentStatus } from "../types/equipment";
import { INCIDENT_STATUS_LABEL, type IncidentStatus } from "../types/incident";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

export default function EquipoDetailPage() {
  const { id } = useParams<{ id: string }>();
  const equipmentId = id!;
  const { role } = useAuth();
  const canManageComponents = role === "Administrador" || role === "Técnico";
  const canManageIncidents = role === "Administrador" || role === "Empleado";
  const isEmployee = role === "Empleado";
  const queryClient = useQueryClient();

  // Para el Empleado, /assignments devuelve solo las suyas: sirve para saber si el equipo es suyo
  const { data: myAssignments } = useAssignments(isEmployee);
  const isAssignedToMe = (myAssignments ?? []).some(
    (a) => a.equipmentId === equipmentId && a.status === "ACTIVE"
  );
  // Administrador reporta en cualquier equipo; Empleado solo en los que tiene asignados
  const canReportIncident = role === "Administrador" || (isEmployee && isAssignedToMe);

  const { data: equipment, isLoading: loadingEquipment } = useQuery({
    queryKey: ["equipment", equipmentId],
    queryFn: () => getEquipmentById(equipmentId),
  });
  const { data: components, isLoading: loadingComponents } = useEquipmentComponents(equipmentId);
  const { data: incidents, isLoading: loadingIncidents } = useEquipmentIncidents(equipmentId);

  // --- Cambio de estado (Administrador / Técnico) ---
  const [newStatus, setNewStatus] = useState<EquipmentStatus | "">("");
  const statusMutation = useMutation({
    mutationFn: async (action: StatusAction): Promise<void> => {
      if (action.kind === "decommission") await deleteEquipment(equipmentId);
      else await changeEquipmentStatus(equipmentId, action.target);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setNewStatus("");
    },
  });

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

  const incidentError = [createIncidentMutation, updateIncidentMutation, deleteIncidentMutation]
    .map((m) => (m.error ? getApiErrorMessage(m.error) : null))
    .find(Boolean);

  if (loadingEquipment) return <p className="p-8">Cargando equipo...</p>;
  if (!equipment) return <p className="p-8 text-red-600">Equipo no encontrado.</p>;

  return (
    <div className="p-8 space-y-8 max-w-3xl">
      <div>
        <Link to="/equipos" className="text-sm text-blue-600">
          ← Volver a equipos
        </Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-xl font-semibold">
          {equipment.internalCode} — {equipment.brand} {equipment.model}
        </h1>
          <div className="flex gap-2 text-sm">
            {canManageComponents && (
              <Link
                to={`/equipos/${equipmentId}/editar`}
                className="rounded bg-blue-600 px-3 py-1.5 text-white"
              >
                Editar equipo
              </Link>
            )}
          </div>
        </div>
        <p className="text-sm text-gray-600">
          {equipment.type} · {equipment.location} · {EQUIPMENT_STATUS_LABEL[equipment.status]}
        </p>
        <dl className="mt-3 grid grid-cols-2 gap-x-6 gap-y-1 text-sm sm:grid-cols-3">
          <div>
            <dt className="text-xs text-gray-500">Número de serie</dt>
            <dd>{equipment.serialNumber}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-500">Fecha de adquisición</dt>
            <dd>{equipment.acquisitionDate ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-xs text-gray-500">Precio de adquisición</dt>
            <dd>
              {equipment.acquisitionPrice != null ? `$${equipment.acquisitionPrice.toFixed(2)}` : "—"}
            </dd>
          </div>
          <div>
            <dt className="text-xs text-gray-500">Garantía hasta</dt>
            <dd>{equipment.warrantyUntil ?? "—"}</dd>
          </div>
        </dl>
        {equipment.operatingSystem && (
          <p className="text-sm text-gray-600">
            <span className="font-medium">Sistema operativo:</span>{" "}
            {equipment.operatingSystem}
          </p>
        )}
      </div>

      {/* Cambio de estado */}
      {canManageComponents && (
        <section className="space-y-2 rounded border p-3 text-sm">
          <h2 className="font-medium">Estado del equipo</h2>
          {manualStatusActions(equipment.status, role, equipment.ownership).length === 0 ? (
            <p className="text-xs text-gray-600">
              Actual: <strong>{EQUIPMENT_STATUS_LABEL[equipment.status]}</strong>.{" "}
              {statusLockedReason(equipment.status)}
            </p>
          ) : (
            <div className="flex flex-wrap items-center gap-2">
              <span>
                Actual: <strong>{EQUIPMENT_STATUS_LABEL[equipment.status]}</strong>
              </span>
              <select
                value={newStatus}
                onChange={(e) => setNewStatus(e.target.value as EquipmentStatus | "")}
                className="rounded border px-2 py-1"
              >
                <option value="">Cambiar a...</option>
                {/* R4: solo los cambios manuales que permite la regla maestra */}
                {manualStatusActions(equipment.status, role, equipment.ownership).map((action) => (
                  <option key={action.target} value={action.target}>
                    {action.label}
                  </option>
                ))}
              </select>
              <button
                disabled={!newStatus || statusMutation.isPending}
                onClick={() => {
                  const action = manualStatusActions(equipment.status, role, equipment.ownership).find(
                    (a) => a.target === newStatus
                  );
                  if (!action) return;
                  if (
                    action.kind === "decommission" &&
                    !confirm("Dar de baja un equipo es definitivo: no podrá volver a servicio. ¿Continuar?")
                  )
                    return;
                  statusMutation.mutate(action);
                }}
                className="rounded bg-blue-600 px-3 py-1 text-white disabled:opacity-50"
              >
                {statusMutation.isPending ? "Guardando..." : "Aplicar"}
              </button>
            </div>
          )}
          {statusMutation.error && (
            <p className="text-xs text-red-600">{getApiErrorMessage(statusMutation.error)}</p>
          )}
        </section>
      )}

      {equipment.ownership === "RENTED" && <RentedEquipmentPanel equipment={equipment} role={role} />}

      {equipment.externalSourceId && (
        <section className="space-y-1 rounded border border-white/10 p-3 text-sm">
          <h2 className="font-medium">Importado de la base de datos de la empresa</h2>
          <p className="text-xs text-gray-400">
            Registro externo {equipment.externalId}. Los datos sincronizados se corrigen en la fuente.
          </p>
          {equipment.externalAttributes && Object.keys(equipment.externalAttributes).length > 0 && (
            <dl className="grid grid-cols-2 gap-x-4 text-xs md:grid-cols-3">
              {Object.entries(equipment.externalAttributes).map(([k, v]) => (
                <div key={k}>
                  <dt className="text-gray-400">{k}</dt>
                  <dd>{typeof v === "string" ? v : JSON.stringify(v)}</dd>
                </div>
              ))}
            </dl>
          )}
        </section>
      )}

      {equipment.status !== "DECOMMISSIONED" && equipment.status !== "RETURNED" && (
        <MaintenancePlansPanel equipmentId={equipmentId} canManage={canManageComponents} />
      )}

      {/* Componentes */}
      <section className="space-y-3">
        <h2 className="font-medium">Componentes</h2>

        {canManageComponents && (
          <form onSubmit={handleCreateComponent} className="flex flex-wrap gap-2 text-sm">
            <input
              placeholder="Tipo (RAM, disco...)"
              value={compForm.componentType}
              onChange={(e) => setCompForm({ ...compForm, componentType: e.target.value })}
              className="input flex-1 min-w-30"
            />
            <input
              placeholder="Marca"
              value={compForm.brand}
              onChange={(e) => setCompForm({ ...compForm, brand: e.target.value })}
              className="input flex-1 min-w-25"
            />
            <input
              placeholder="Modelo"
              value={compForm.model}
              onChange={(e) => setCompForm({ ...compForm, model: e.target.value })}
              className="input flex-1 min-w-25"
            />
            <input
              placeholder="Serie"
              value={compForm.serialNumber}
              onChange={(e) => setCompForm({ ...compForm, serialNumber: e.target.value })}
              className="input flex-1 min-w-25"
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

        {isEmployee && !isAssignedToMe && (
          <p className="text-sm text-gray-500">
            Solo puedes reportar fallas de equipos que tienes asignados.
          </p>
        )}
        {canReportIncident && (
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
                  {canManageIncidents && !inc.maintenanceId ? (
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
                      {Object.entries(INCIDENT_STATUS_LABEL)
                        .filter(([value]) => value !== "IN_PROGRESS" || inc.status === "IN_PROGRESS")
                        .map(([value, label]) => (
                          <option key={value} value={value}>
                            {label}
                          </option>
                        ))}
                    </select>
                  ) : (
                    <span className="text-xs">{INCIDENT_STATUS_LABEL[inc.status]}</span>
                  )}
                  {canManageIncidents && !inc.maintenanceId && (
                    <button
                      onClick={() => {
                        if (confirm("¿Eliminar este incidente?")) deleteIncidentMutation.mutate(inc.id);
                      }}
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

    </div>
  );
}
