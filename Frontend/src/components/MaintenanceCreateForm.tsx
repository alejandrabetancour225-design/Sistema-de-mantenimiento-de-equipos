import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useSpareParts } from "../hooks/useMaintenance";
import { useEquipmentIncidents } from "../hooks/useEquipmentDetail";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { useUsers } from "../hooks/useUsers";
import {
  createMaintenance,
  createMaintenanceFromIncident,
  updateMaintenance,
  closeMaintenance,
  addSparePart,
} from "../services/maintenanceServices";
import { MAINTENANCE_TYPE_LABEL, type MaintenanceType } from "../types/maintenance";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";
import { Field } from "./MaintenanceShared";
import EquipmentCodeSearch from "./EquipmentCodeSearch";
import { useInvalidateAll } from "../lib/maintenanceUtils";

export default function MaintenanceCreateForm({ onCreated }: { onCreated?: () => void }) {
  const { role, userId, fullName } = useAuth();
  const isAdmin = role === "Administrador";
  const invalidateAll = useInvalidateAll();
  const [equipmentId, setEquipmentId] = useState("");
  const { data: equipmentList } = useEquipmentList();

  const { data: incidents } = useEquipmentIncidents(equipmentId);
  const { data: spareParts } = useSpareParts();
  // Solo Admin tiene acceso a /users; un Técnico se asigna a sí mismo.
  const { data: users } = useUsers(isAdmin);
  const technicians = (users ?? []).filter(
    (u) => u.active && (u.role === "Técnico" || u.role === "Administrador")
  );

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
      setEquipmentId("");
      onCreated?.();
    },
  });

  const fromIncidentMutation = useMutation({
    mutationFn: createMaintenanceFromIncident,
    onSuccess: () => {
      invalidateAll();
      onCreated?.();
    },
  });

  function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setWarnings([]);
    if (!equipmentId) return setFormError("Selecciona el equipo.");
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
    <section className="space-y-3 rounded border bg-white p-4">
      <h2 className="font-medium">Nuevo mantenimiento</h2>

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
        <div className="space-y-1">
          <span className="text-xs text-gray-600">Equipo * (busca por código)</span>
          <EquipmentCodeSearch
            equipment={equipmentList ?? []}
            value={equipmentId}
            onChange={(id) => {
              setEquipmentId(id);
              setForm((f) => ({ ...f, incidentId: "" }));
              setFormError("");
            }}
          />
          <span className="text-xs text-gray-500">
            Los equipos en mantenimiento o dados de baja aparecen, pero no se pueden elegir.
          </span>
        </div>
        {!equipmentId && (
          <p className="text-xs text-gray-500">Elige un equipo para continuar con el registro.</p>
        )}

        {equipmentId && (
          <>
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
          </>
        )}
      </form>
    </section>
  );
}
