import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import {
  updateMaintenance,
  closeMaintenance,
  addSparePart,
  removeSparePart,
  deleteMaintenance,
} from "../services/maintenanceServices";
import {
  MAINTENANCE_STATUS_LABEL,
  type Maintenance,
  type MaintenanceStatus,
} from "../types/maintenance";
import { EQUIPMENT_STATUS_LABEL, type EquipmentStatus } from "../types/equipment";
import { INCIDENT_STATUS_LABEL } from "../types/incident";
import { getApiErrorMessage } from "../lib/errors";
import { Field } from "./MaintenanceShared";
import { money, useInvalidateAll } from "../lib/maintenanceUtils";

// El backend no admite UNDER_MAINTENANCE como estado final del equipo.
const FINAL_EQUIPMENT_STATUSES: EquipmentStatus[] = [
  "AVAILABLE",
  "IN_USE",
  "OUT_OF_SERVICE",
  "DECOMMISSIONED",
];
// PutMaintenance no acepta COMPLETED (se completa con CloseMaintenance).
const EDITABLE_STATUSES: MaintenanceStatus[] = ["OPEN", "IN_PROGRESS", "CANCELLED"];

type Mode = "view" | "edit" | "close";

export default function MaintenanceDetail({
  maintenance: m,
  isAdmin,
  technicians,
  availableSpareParts,
}: {
  maintenance: Maintenance;
  isAdmin: boolean;
  technicians: { id: string; fullName: string; role?: string }[];
  availableSpareParts: { id: string; name: string; active: boolean }[];
}) {
  const invalidateAll = useInvalidateAll();
  const [mode, setMode] = useState<Mode>("view");
  const [selectedPart, setSelectedPart] = useState("");
  const [quantity, setQuantity] = useState(1);

  const isClosed = m.status === "COMPLETED" || m.status === "CANCELLED";

  // --- Edición (PutMaintenance) ---
  const [edit, setEdit] = useState({
    status: m.status as MaintenanceStatus,
    reportedProblem: m.reportedProblem,
    workDone: m.workDone ?? "",
    laborCost: String(m.laborCost),
    otherCosts: String(m.otherCosts),
    nextMaintenanceDate: m.nextMaintenanceDate ?? "",
    observations: m.observations ?? "",
    technicianId: m.technicianId,
  });

  const updateMutation = useMutation({
    mutationFn: () =>
      updateMaintenance(m.id, {
        status: edit.status,
        reportedProblem: edit.reportedProblem.trim(),
        workDone: edit.workDone.trim() || undefined,
        laborCost: Number(edit.laborCost) || 0,
        otherCosts: Number(edit.otherCosts) || 0,
        nextMaintenanceDate: edit.nextMaintenanceDate || undefined,
        observations: edit.observations,
        technicianId: isAdmin ? edit.technicianId : undefined,
      }),
    onSuccess: () => {
      invalidateAll();
      setMode("view");
    },
  });

  function handleSave() {
    if (edit.status === "CANCELLED" && m.status !== "CANCELLED") {
      const ok = confirm(
        "Cancelar libera el equipo y devuelve el incidente a Abierto. Esto no se puede deshacer. ¿Continuar?"
      );
      if (!ok) return;
    }
    updateMutation.mutate();
  }

  // --- Cierre (CloseMaintenance) ---
  const [close, setClose] = useState({
    workDone: m.workDone ?? "",
    laborCost: String(m.laborCost),
    otherCosts: String(m.otherCosts),
    nextMaintenanceDate: m.nextMaintenanceDate ?? "",
    observations: m.observations ?? "",
    equipmentFinalStatus: "AVAILABLE" as EquipmentStatus,
    incidentFinalStatus: "RESOLVED" as "RESOLVED" | "CLOSED",
  });

  const closeMutation = useMutation({
    mutationFn: () =>
      closeMaintenance(m.id, {
        workDone: close.workDone.trim(),
        laborCost: Number(close.laborCost) || 0,
        otherCosts: Number(close.otherCosts) || 0,
        nextMaintenanceDate: close.nextMaintenanceDate || undefined,
        observations: close.observations.trim() || undefined,
        equipmentFinalStatus: close.equipmentFinalStatus,
        incidentFinalStatus: m.incidentId ? close.incidentFinalStatus : undefined,
      }),
    onSuccess: () => {
      invalidateAll();
      setMode("view");
    },
  });

  // --- Repuestos ---
  const addPartMutation = useMutation({
    mutationFn: () => addSparePart(m.id, { sparePartId: selectedPart, quantity }),
    onSuccess: () => {
      invalidateAll();
      setSelectedPart("");
      setQuantity(1);
    },
  });
  const removePartMutation = useMutation({
    mutationFn: (maintenanceSparePartId: string) => removeSparePart(m.id, maintenanceSparePartId),
    onSuccess: invalidateAll,
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteMaintenance(m.id),
    onSuccess: invalidateAll,
  });

  const actionError =
    updateMutation.error ?? closeMutation.error ?? addPartMutation.error ?? removePartMutation.error ?? deleteMutation.error;

  return (
    <div className="space-y-3 border-t pt-2">
      {/* Datos de solo lectura */}
      <div className="space-y-0.5 text-xs">
        <p>
          <span className="font-medium">Costos:</span> mano de obra {money(m.laborCost)} · repuestos{" "}
          {money(m.sparePartsCost)} · otros {money(m.otherCosts)}
        </p>
        {m.workDone && (
          <p>
            <span className="font-medium">Trabajo realizado:</span> {m.workDone}
          </p>
        )}
        {m.observations && (
          <p>
            <span className="font-medium">Observaciones:</span> {m.observations}
          </p>
        )}
        {m.incidentId && <p className="text-gray-500">Vinculado a un incidente.</p>}
      </div>

      {/* Repuestos */}
      <div>
        <p className="mb-1 text-xs font-medium">Repuestos usados</p>
        <ul className="space-y-1 text-xs">
          {m.spareParts.map((sp) => (
            <li key={sp.id} className="flex justify-between">
              <span>
                {sp.sparePartName} x{sp.quantity} — {money(sp.subtotal)}
              </span>
              {!isClosed && (
                <button onClick={() => removePartMutation.mutate(sp.id)} className="text-red-600">
                  Quitar
                </button>
              )}
            </li>
          ))}
          {m.spareParts.length === 0 && <li className="text-gray-500">Ninguno</li>}
        </ul>

        {!isClosed && (
          <div className="mt-2 flex gap-2">
            <select
              value={selectedPart}
              onChange={(e) => setSelectedPart(e.target.value)}
              className="input flex-1 text-xs"
            >
              <option value="">Repuesto...</option>
              {availableSpareParts
                .filter((sp) => sp.active)
                .map((sp) => (
                  <option key={sp.id} value={sp.id}>
                    {sp.name}
                  </option>
                ))}
            </select>
            <input
              type="number"
              min={1}
              value={quantity}
              onChange={(e) => setQuantity(Number(e.target.value))}
              className="input w-16 text-xs"
            />
            <button
              disabled={!selectedPart || quantity < 1 || addPartMutation.isPending}
              onClick={() => addPartMutation.mutate()}
              className="rounded bg-gray-700 px-2 py-1 text-xs text-white disabled:opacity-50"
            >
              Agregar
            </button>
          </div>
        )}
      </div>

      {/* Editar */}
      {mode === "edit" && !isClosed && (
        <div className="space-y-2 rounded border bg-gray-50 p-3 text-xs">
          <p className="font-medium">Editar mantenimiento</p>
          <div className="flex flex-wrap gap-2">
            <Field label="Estado">
              <select
                value={edit.status}
                onChange={(e) => setEdit({ ...edit, status: e.target.value as MaintenanceStatus })}
                className="input"
              >
                {EDITABLE_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {MAINTENANCE_STATUS_LABEL[s]}
                  </option>
                ))}
              </select>
            </Field>
            {isAdmin && (
              <Field label="Técnico">
                <select
                  value={edit.technicianId}
                  onChange={(e) => setEdit({ ...edit, technicianId: e.target.value })}
                  className="input"
                >
                  {technicians.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.fullName}
                    </option>
                  ))}
                </select>
              </Field>
            )}
          </div>
          <Field label="Problema reportado">
            <input
              value={edit.reportedProblem}
              onChange={(e) => setEdit({ ...edit, reportedProblem: e.target.value })}
              className="input"
            />
          </Field>
          <Field label="Trabajo realizado (avance)">
            <input
              value={edit.workDone}
              onChange={(e) => setEdit({ ...edit, workDone: e.target.value })}
              className="input"
            />
          </Field>
          <div className="flex flex-wrap gap-2">
            <Field label="Mano de obra">
              <input
                type="number"
                min={0}
                step="0.01"
                value={edit.laborCost}
                onChange={(e) => setEdit({ ...edit, laborCost: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="Otros costos">
              <input
                type="number"
                min={0}
                step="0.01"
                value={edit.otherCosts}
                onChange={(e) => setEdit({ ...edit, otherCosts: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="Próximo mantenimiento">
              <input
                type="date"
                value={edit.nextMaintenanceDate}
                onChange={(e) => setEdit({ ...edit, nextMaintenanceDate: e.target.value })}
                className="input"
              />
            </Field>
          </div>
          <Field label="Observaciones">
            <input
              value={edit.observations}
              onChange={(e) => setEdit({ ...edit, observations: e.target.value })}
              className="input"
            />
          </Field>
          <div className="flex gap-2">
            <button
              disabled={updateMutation.isPending || edit.reportedProblem.trim().length < 3}
              onClick={handleSave}
              className="rounded bg-blue-600 px-3 py-1 text-white disabled:opacity-50"
            >
              Guardar cambios
            </button>
            <button onClick={() => setMode("view")} className="text-gray-600">
              Cancelar edición
            </button>
          </div>
        </div>
      )}

      {/* Cerrar */}
      {mode === "close" && !isClosed && (
        <div className="space-y-2 rounded border bg-green-50 p-3 text-xs">
          <p className="font-medium">Cerrar mantenimiento</p>
          <Field label="Trabajo realizado (obligatorio, mín. 5 caracteres)">
            <input
              value={close.workDone}
              onChange={(e) => setClose({ ...close, workDone: e.target.value })}
              className="input"
            />
          </Field>
          <div className="flex flex-wrap gap-2">
            <Field label="Mano de obra">
              <input
                type="number"
                min={0}
                step="0.01"
                value={close.laborCost}
                onChange={(e) => setClose({ ...close, laborCost: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="Otros costos">
              <input
                type="number"
                min={0}
                step="0.01"
                value={close.otherCosts}
                onChange={(e) => setClose({ ...close, otherCosts: e.target.value })}
                className="input"
              />
            </Field>
            <Field label="Próximo mantenimiento">
              <input
                type="date"
                value={close.nextMaintenanceDate}
                onChange={(e) => setClose({ ...close, nextMaintenanceDate: e.target.value })}
                className="input"
              />
            </Field>
          </div>
          <div className="flex flex-wrap gap-2">
            <Field label="Estado final del equipo">
              <select
                value={close.equipmentFinalStatus}
                onChange={(e) =>
                  setClose({ ...close, equipmentFinalStatus: e.target.value as EquipmentStatus })
                }
                className="input"
              >
                {FINAL_EQUIPMENT_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {EQUIPMENT_STATUS_LABEL[s]}
                  </option>
                ))}
              </select>
            </Field>
            {m.incidentId && (
              <Field label="Estado final del incidente">
                <select
                  value={close.incidentFinalStatus}
                  onChange={(e) =>
                    setClose({ ...close, incidentFinalStatus: e.target.value as "RESOLVED" | "CLOSED" })
                  }
                  className="input"
                >
                  <option value="RESOLVED">{INCIDENT_STATUS_LABEL.RESOLVED}</option>
                  <option value="CLOSED">{INCIDENT_STATUS_LABEL.CLOSED}</option>
                </select>
              </Field>
            )}
          </div>
          <Field label="Observaciones">
            <input
              value={close.observations}
              onChange={(e) => setClose({ ...close, observations: e.target.value })}
              className="input"
            />
          </Field>
          <div className="flex gap-2">
            <button
              disabled={close.workDone.trim().length < 5 || closeMutation.isPending}
              onClick={() => closeMutation.mutate()}
              className="rounded bg-green-600 px-3 py-1 text-white disabled:opacity-50"
            >
              Confirmar cierre
            </button>
            <button onClick={() => setMode("view")} className="text-gray-600">
              Cancelar
            </button>
          </div>
        </div>
      )}

      {actionError && <p className="text-xs text-red-600">{getApiErrorMessage(actionError)}</p>}

      {!isClosed && mode === "view" && (
        <div className="flex flex-wrap gap-3 text-xs">
          <button onClick={() => setMode("edit")} className="rounded bg-blue-600 px-3 py-1 text-white">
            Editar / cambiar estado
          </button>
          <button onClick={() => setMode("close")} className="rounded bg-green-600 px-3 py-1 text-white">
            Cerrar mantenimiento
          </button>
          <button
            onClick={() => {
              if (confirm("¿Eliminar este mantenimiento?")) deleteMutation.mutate();
            }}
            className="text-red-600"
          >
            Eliminar mantenimiento
          </button>
        </div>
      )}
    </div>
  );
}
