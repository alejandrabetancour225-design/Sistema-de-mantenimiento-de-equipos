import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  useEquipmentHistory,
  useMaintenancesByEquipment,
  useSpareParts,
} from "../hooks/useMaintenance";
import { useEquipmentIncidents } from "../hooks/useEquipmentDetail";
import { useUsers } from "../hooks/useUsers";
import {
  createMaintenance,
  createMaintenanceFromIncident,
  updateMaintenance,
  closeMaintenance,
  addSparePart,
  removeSparePart,
  deleteMaintenance,
} from "../services/maintenanceServices";
import {
  MAINTENANCE_TYPE_LABEL,
  MAINTENANCE_STATUS_LABEL,
  type Maintenance,
  type MaintenanceStatus,
  type MaintenanceType,
} from "../types/maintenance";
import { EQUIPMENT_STATUS_LABEL, type EquipmentStatus } from "../types/equipment";
import { INCIDENT_STATUS_LABEL } from "../types/incident";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

const money = (n: number) => `$${n.toFixed(2)}`;

// El backend no admite UNDER_MAINTENANCE como estado final del equipo.
const FINAL_EQUIPMENT_STATUSES: EquipmentStatus[] = [
  "AVAILABLE",
  "IN_USE",
  "OUT_OF_SERVICE",
  "DECOMMISSIONED",
];
// PutMaintenance no acepta COMPLETED (se completa con CloseMaintenance).
const EDITABLE_STATUSES: MaintenanceStatus[] = ["OPEN", "IN_PROGRESS", "CANCELLED"];

// Cualquier cambio de mantenimiento puede mover equipo, incidente y dashboard.
function useInvalidateAll(equipmentId: string) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: ["maintenances"] });
    queryClient.invalidateQueries({ queryKey: ["equipment"] });
    queryClient.invalidateQueries({ queryKey: ["equipment-incidents", equipmentId] });
    queryClient.invalidateQueries({ queryKey: ["dashboard"] });
  };
}

export default function MaintenanceSection({ equipmentId }: { equipmentId: string }) {
  const { role, userId, fullName } = useAuth();
  const isAdmin = role === "Administrador";
  const invalidateAll = useInvalidateAll(equipmentId);

  const { data: maintenances, isLoading } = useMaintenancesByEquipment(equipmentId);
  const { data: history } = useEquipmentHistory(equipmentId);
  const { data: incidents } = useEquipmentIncidents(equipmentId);
  const { data: spareParts } = useSpareParts();
  // Solo Admin tiene acceso a /users; un Técnico se asigna a sí mismo.
  const { data: users } = useUsers(isAdmin);
  const technicians = (users ?? []).filter(
    (u) => u.active && (u.role === "Técnico" || u.role === "Administrador")
  );

  const [openId, setOpenId] = useState<string | null>(null);

  const emptyForm = {
    technicianId: isAdmin ? "" : userId ?? "",
    type: "CORRECTIVE" as MaintenanceType,
    incidentId: "",
    reportedProblem: "",
    laborCost: "0",
    otherCosts: "0",
    startedAt: "",
    nextMaintenanceDate: "",
    observations: "",
    workDone: "",
    complete: false,
  };
  const [form, setForm] = useState(emptyForm);

  // Repuestos que se registran junto con el mantenimiento
  const [parts, setParts] = useState<{ sparePartId: string; quantity: number }[]>([]);
  const [partId, setPartId] = useState("");
  const [partQty, setPartQty] = useState(1);
  const [formError, setFormError] = useState("");
  const [warnings, setWarnings] = useState<string[]>([]);

  function addPartToForm() {
    if (!partId || partQty < 1) return;
    setParts((prev) => {
      const existing = prev.find((p) => p.sparePartId === partId);
      // El backend no admite el mismo repuesto dos veces en un mantenimiento: se suma la cantidad
      return existing
        ? prev.map((p) =>
            p.sparePartId === partId ? { ...p, quantity: p.quantity + partQty } : p
          )
        : [...prev, { sparePartId: partId, quantity: partQty }];
    });
    setPartId("");
    setPartQty(1);
  }

  // Incidentes que todavía se pueden convertir en mantenimiento
  const linkableIncidents = (incidents ?? []).filter(
    (i) => i.status === "OPEN" && !i.maintenanceId
  );

  // Registrar = crear el mantenimiento y encadenar repuestos, trabajo realizado y cierre.
  // El backend los expone por separado, así que si un paso posterior falla, el mantenimiento
  // ya quedó creado y se avisa qué falta (se puede completar desde "Detalle").
  const createMutation = useMutation({
    mutationFn: async () => {
      const created = await createMaintenance({
        equipmentId,
        technicianId: form.technicianId,
        type: form.type,
        incidentId: form.incidentId || undefined,
        reportedProblem: form.reportedProblem.trim(),
        laborCost: Number(form.laborCost) || 0,
        otherCosts: Number(form.otherCosts) || 0,
        startedAt: form.startedAt || undefined,
        nextMaintenanceDate: form.nextMaintenanceDate || undefined,
        observations: form.observations.trim() || undefined,
      });

      const problems: string[] = [];
      for (const p of parts) {
        try {
          await addSparePart(created.id, p);
        } catch (e) {
          problems.push(`Repuesto no agregado: ${getApiErrorMessage(e)}`);
        }
      }

      const workDone = form.workDone.trim();
      if (form.complete) {
        try {
          await closeMaintenance(created.id, {
            workDone,
            laborCost: Number(form.laborCost) || 0,
            otherCosts: Number(form.otherCosts) || 0,
            nextMaintenanceDate: form.nextMaintenanceDate || undefined,
            observations: form.observations.trim() || undefined,
          });
        } catch (e) {
          problems.push(`No se pudo cerrar el mantenimiento: ${getApiErrorMessage(e)}`);
        }
      } else if (workDone) {
        try {
          await updateMaintenance(created.id, { workDone });
        } catch (e) {
          problems.push(`No se guardó el trabajo realizado: ${getApiErrorMessage(e)}`);
        }
      }
      return problems;
    },
    onSuccess: (problems) => {
      invalidateAll();
      setWarnings(problems);
      setForm(emptyForm);
      setParts([]);
      setFormError("");
    },
  });

  const fromIncidentMutation = useMutation({
    mutationFn: createMaintenanceFromIncident,
    onSuccess: (m) => {
      invalidateAll();
      setOpenId(m.id);
    },
  });

  function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setWarnings([]);
    if (!form.technicianId) return setFormError("Selecciona el técnico responsable.");
    if (form.reportedProblem.trim().length < 3) return setFormError("Describe el problema (mínimo 3 caracteres).");
    if (form.complete && form.workDone.trim().length < 5)
      return setFormError("Para registrarlo como completado describe el trabajo realizado (mínimo 5 caracteres).");
    setFormError("");
    createMutation.mutate();
  }

  function handlePickIncident(id: string) {
    const inc = linkableIncidents.find((i) => i.id === id);
    setForm((f) => ({
      ...f,
      incidentId: id,
      // Si no escribió nada, sugerimos la descripción del incidente
      reportedProblem: !f.reportedProblem && inc ? inc.description : f.reportedProblem,
    }));
  }

  return (
    <section className="space-y-3">
      <h2 className="font-medium">Mantenimientos</h2>

      {/* Costo acumulado del equipo (GetHistoryByEquipment) */}
      {history && (
        <div className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-4">
          <CostBox label="Mano de obra" value={history.totalLaborCost} />
          <CostBox label="Repuestos" value={history.totalSparePartsCost} />
          <CostBox label="Otros" value={history.totalOtherCosts} />
          <CostBox label="Costo total acumulado" value={history.totalCost} strong />
        </div>
      )}

      {/* Crear desde incidente (CreateFromIncident) */}
      {linkableIncidents.length > 0 && (
        <div className="space-y-2 rounded border border-yellow-300 bg-yellow-50 p-3 text-sm">
          <p className="font-medium">Incidentes abiertos sin mantenimiento</p>
          <ul className="space-y-1">
            {linkableIncidents.map((inc) => (
              <li key={inc.id} className="flex items-center justify-between gap-2">
                <span className="text-xs">{inc.description}</span>
                <button
                  disabled={fromIncidentMutation.isPending}
                  onClick={() => fromIncidentMutation.mutate(inc.id)}
                  className="shrink-0 rounded bg-yellow-600 px-2 py-1 text-xs text-white disabled:opacity-50"
                >
                  Crear mantenimiento
                </button>
              </li>
            ))}
          </ul>
          <p className="text-xs text-gray-600">
            Se crea correctivo y a tu nombre como técnico; luego puedes editarlo.
          </p>
          {fromIncidentMutation.error && (
            <p className="text-xs text-red-600">{getApiErrorMessage(fromIncidentMutation.error)}</p>
          )}
        </div>
      )}

      <form onSubmit={handleCreate} className="space-y-2 rounded border p-3 text-sm">
        <div className="flex flex-wrap gap-2">
          {!isAdmin && (
            <div className="min-w-[160px] flex-1 rounded border bg-gray-50 px-3 py-2 text-xs text-gray-600">
              Técnico responsable: <strong>{fullName ?? "tú"}</strong>
            </div>
          )}
          {isAdmin && (
            <select
              value={form.technicianId}
              onChange={(e) => setForm({ ...form, technicianId: e.target.value })}
              className="input min-w-[160px] flex-1"
            >
              <option value="">Selecciona técnico</option>
              {technicians.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.fullName} ({t.role})
                </option>
              ))}
            </select>
          )}
          <select
            value={form.type}
            onChange={(e) => setForm({ ...form, type: e.target.value as MaintenanceType })}
            className="input min-w-[140px] flex-1"
          >
            {Object.entries(MAINTENANCE_TYPE_LABEL).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
          <select
            value={form.incidentId}
            onChange={(e) => handlePickIncident(e.target.value)}
            className="input min-w-[180px] flex-1"
          >
            <option value="">Sin incidente vinculado</option>
            {linkableIncidents.map((i) => (
              <option key={i.id} value={i.id}>
                {i.description.slice(0, 50)}
              </option>
            ))}
          </select>
        </div>

        <input
          placeholder="Problema reportado"
          value={form.reportedProblem}
          onChange={(e) => setForm({ ...form, reportedProblem: e.target.value })}
          className="input w-full"
        />

        <div className="flex flex-wrap gap-2">
          <Field label="Mano de obra">
            <input
              type="number"
              min={0}
              step="0.01"
              value={form.laborCost}
              onChange={(e) => setForm({ ...form, laborCost: e.target.value })}
              className="input"
            />
          </Field>
          <Field label="Otros costos">
            <input
              type="number"
              min={0}
              step="0.01"
              value={form.otherCosts}
              onChange={(e) => setForm({ ...form, otherCosts: e.target.value })}
              className="input"
            />
          </Field>
          <Field label="Fecha del mantenimiento">
            <input
              type="date"
              max={new Date().toISOString().slice(0, 10)}
              value={form.startedAt}
              onChange={(e) => setForm({ ...form, startedAt: e.target.value })}
              className="input"
            />
          </Field>
          <Field label="Próximo mantenimiento">
            <input
              type="date"
              value={form.nextMaintenanceDate}
              onChange={(e) => setForm({ ...form, nextMaintenanceDate: e.target.value })}
              className="input"
            />
          </Field>
        </div>

        <input
          placeholder="Trabajo realizado (opcional si aún está en curso)"
          value={form.workDone}
          onChange={(e) => setForm({ ...form, workDone: e.target.value })}
          className="input w-full"
        />

        <div className="space-y-1 rounded border bg-gray-50 p-2">
          <p className="text-xs font-medium">Repuestos usados (opcional)</p>
          <div className="flex gap-2">
            <select
              value={partId}
              onChange={(e) => setPartId(e.target.value)}
              className="input flex-1 text-xs"
            >
              <option value="">Repuesto...</option>
              {(spareParts ?? [])
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
              value={partQty}
              onChange={(e) => setPartQty(Number(e.target.value))}
              className="input w-16 text-xs"
            />
            <button
              type="button"
              disabled={!partId || partQty < 1}
              onClick={addPartToForm}
              className="rounded bg-gray-700 px-2 py-1 text-xs text-white disabled:opacity-50"
            >
              Agregar
            </button>
          </div>
          {parts.length > 0 && (
            <ul className="space-y-0.5 text-xs">
              {parts.map((p) => (
                <li key={p.sparePartId} className="flex justify-between">
                  <span>
                    {(spareParts ?? []).find((sp) => sp.id === p.sparePartId)?.name ?? "Repuesto"} x
                    {p.quantity}
                  </span>
                  <button
                    type="button"
                    onClick={() => setParts(parts.filter((x) => x.sparePartId !== p.sparePartId))}
                    className="text-red-600"
                  >
                    Quitar
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <input
          placeholder="Observaciones (opcional)"
          value={form.observations}
          onChange={(e) => setForm({ ...form, observations: e.target.value })}
          className="input w-full"
        />

        <label className="flex items-center gap-2 text-xs">
          <input
            type="checkbox"
            checked={form.complete}
            onChange={(e) => setForm({ ...form, complete: e.target.checked })}
          />
          Registrar como completado (cierra el mantenimiento y deja el equipo Disponible)
        </label>

        {formError && <p className="text-xs text-red-600">{formError}</p>}
        {createMutation.error && (
          <p className="text-xs text-red-600">{getApiErrorMessage(createMutation.error)}</p>
        )}
        {warnings.length > 0 && (
          <div className="rounded border border-yellow-300 bg-yellow-50 p-2 text-xs">
            <p className="font-medium">El mantenimiento se creó, pero:</p>
            <ul className="list-disc pl-4">
              {warnings.map((w) => (
                <li key={w}>{w}</li>
              ))}
            </ul>
            <p>Puedes completarlo desde "Detalle" en la lista de abajo.</p>
          </div>
        )}

        <button
          type="submit"
          disabled={createMutation.isPending}
          className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-50"
        >
          Registrar mantenimiento
        </button>
      </form>

      {isLoading ? (
        <p className="text-sm text-gray-500">Cargando...</p>
      ) : (
        <ul className="space-y-2">
          {maintenances?.map((m) => (
            <li key={m.id} className="space-y-2 rounded border p-3 text-sm">
              <div className="flex items-start justify-between">
                <div>
                  <p className="font-medium">
                    {MAINTENANCE_TYPE_LABEL[m.type]} — {m.reportedProblem}
                  </p>
                  <p className="text-xs text-gray-500">
                    Técnico: {m.technicianName} · {MAINTENANCE_STATUS_LABEL[m.status]} · Total:{" "}
                    {money(m.totalCost)}
                    {m.nextMaintenanceDate && ` · Próximo: ${m.nextMaintenanceDate}`}
                  </p>
                </div>
                <button
                  className="text-xs text-blue-600"
                  onClick={() => setOpenId(openId === m.id ? null : m.id)}
                >
                  {openId === m.id ? "Ocultar" : "Detalle"}
                </button>
              </div>

              {openId === m.id && (
                <MaintenanceDetail
                  maintenance={m}
                  equipmentId={equipmentId}
                  isAdmin={isAdmin}
                  technicians={technicians}
                  availableSpareParts={spareParts ?? []}
                />
              )}
            </li>
          ))}
          {maintenances?.length === 0 && (
            <p className="text-sm text-gray-500">Sin mantenimientos registrados.</p>
          )}
        </ul>
      )}
    </section>
  );
}

function CostBox({ label, value, strong }: { label: string; value: number; strong?: boolean }) {
  return (
    <div className={`rounded border p-2 ${strong ? "border-blue-300 bg-blue-50" : ""}`}>
      <p className="text-xs text-gray-500">{label}</p>
      <p className={strong ? "font-semibold" : ""}>{money(value)}</p>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block min-w-[140px] flex-1">
      <span className="text-xs text-gray-600">{label}</span>
      {children}
    </label>
  );
}

type Mode = "view" | "edit" | "close";

function MaintenanceDetail({
  maintenance: m,
  equipmentId,
  isAdmin,
  technicians,
  availableSpareParts,
}: {
  maintenance: Maintenance;
  equipmentId: string;
  isAdmin: boolean;
  technicians: { id: string; fullName: string; role?: string }[];
  availableSpareParts: { id: string; name: string; active: boolean }[];
}) {
  const invalidateAll = useInvalidateAll(equipmentId);
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