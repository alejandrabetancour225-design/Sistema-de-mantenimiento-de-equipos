import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createPlan, getPlanTechnicians, getPlans, updatePlan, type MaintenancePlan } from "../services/planServices";
import { getApiErrorMessage } from "../lib/errors";
import { todayInBogota } from "../schemas/equipmentSchemas";

// Planes de mantenimiento preventivo del equipo (cada N semanas).
export default function MaintenancePlansPanel({ equipmentId, canManage }: { equipmentId: string; canManage: boolean }) {
  const queryClient = useQueryClient();
  const { data: plans } = useQuery({ queryKey: ["plans", equipmentId], queryFn: () => getPlans(equipmentId) });
  const { data: technicians } = useQuery({
    queryKey: ["plan-technicians"],
    queryFn: getPlanTechnicians,
    enabled: canManage,
  });
  const [form, setForm] = useState({ description: "", intervalWeeks: 12, nextDate: "", technicianId: "" });
  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["plans"] });
    queryClient.invalidateQueries({ queryKey: ["calendar"] });
  };
  const create = useMutation({
    mutationFn: () =>
      createPlan({
        equipmentId,
        description: form.description,
        intervalWeeks: form.intervalWeeks,
        nextDate: form.nextDate,
        technicianId: form.technicianId || null,
        active: true,
      }),
    onSuccess: () => {
      refresh();
      setForm({ description: "", intervalWeeks: 12, nextDate: "", technicianId: "" });
    },
  });
  const toggle = useMutation({
    mutationFn: (p: MaintenancePlan) =>
      updatePlan(p.id, {
        equipmentId,
        description: p.description,
        intervalWeeks: p.intervalWeeks,
        nextDate: p.nextDate,
        technicianId: p.technicianId,
        active: !p.active,
      }),
    onSuccess: refresh,
  });

  return (
    <section className="space-y-2 text-sm">
      <h2 className="font-medium">Plan de mantenimiento preventivo</h2>
      <ul className="divide-y rounded border">
        {plans?.map((p) => (
          <li key={p.id} className={`flex items-center justify-between gap-2 px-3 py-2 ${p.active ? "" : "opacity-50"}`}>
            <div>
              <p>{p.description} · cada {p.intervalWeeks} semana{p.intervalWeeks === 1 ? "" : "s"}</p>
              <p className={`text-xs ${p.overdue ? "text-red-500" : "text-gray-500"}`}>
                Próximo: {p.nextDate}
                {p.overdue ? " (vencido)" : ""}
                {p.technicianName ? ` · Responsable: ${p.technicianName}` : ""}
              </p>
            </div>
            {canManage && (
              <button onClick={() => toggle.mutate(p)} className="text-xs text-blue-500">
                {p.active ? "Pausar" : "Reactivar"}
              </button>
            )}
          </li>
        ))}
        {plans?.length === 0 && <li className="px-3 py-2 text-gray-500">Sin plan preventivo.</li>}
      </ul>

      {canManage && (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            create.mutate();
          }}
          className="grid gap-2 md:grid-cols-5"
        >
          <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })}
            placeholder="Qué se hace (ej: limpieza y revisión)" className="input md:col-span-2" required />
          <label className="flex items-center gap-1 text-xs">
            Cada
            <input type="number" min={1} max={104} value={form.intervalWeeks}
              onChange={(e) => setForm({ ...form, intervalWeeks: Number(e.target.value) })} className="input w-16" />
            semanas
          </label>
          <input type="date" min={todayInBogota()} value={form.nextDate}
            onChange={(e) => setForm({ ...form, nextDate: e.target.value })} className="input" required />
          <select value={form.technicianId} onChange={(e) => setForm({ ...form, technicianId: e.target.value })} className="input">
            <option value="">Responsable (opcional)</option>
            {technicians?.map((t) => (
              <option key={t.id} value={t.id}>{t.fullName}</option>
            ))}
          </select>
          <button type="submit" disabled={create.isPending} className="rounded bg-blue-600 px-3 py-1 text-white disabled:opacity-50 md:col-span-5">
            Programar
          </button>
        </form>
      )}
      {(create.error || toggle.error) && (
        <p className="text-xs text-red-500">{getApiErrorMessage(create.error ?? toggle.error)}</p>
      )}
    </section>
  );
}
