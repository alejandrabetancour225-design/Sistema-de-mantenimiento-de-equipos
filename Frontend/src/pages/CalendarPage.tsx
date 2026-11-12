import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import interactionPlugin from "@fullcalendar/interaction";
import esLocale from "@fullcalendar/core/locales/es";
import type { DatesSetArg, EventClickArg, EventDropArg } from "@fullcalendar/core";
import { getCalendar, reschedulePlan, startPlan, type CalendarEvent } from "../services/planServices";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

const COLORS: Record<CalendarEvent["kind"], string> = {
  plan: "#2563eb",
  open: "#d97706",
  done: "#16a34a",
  contract: "#9333ea",
};

const LEGEND: [CalendarEvent["kind"] | "overdue", string, string][] = [
  ["plan", "Preventivo programado", COLORS.plan],
  ["overdue", "Vencido", "#dc2626"],
  ["open", "Mantenimiento en curso", COLORS.open],
  ["done", "Completado", COLORS.done],
  ["contract", "Vence contrato de alquiler", COLORS.contract],
];

// Cronograma de mantenimiento (Q11, Q38). Técnico y Administrador pueden arrastrar la próxima
// fecha de un plan e iniciar el mantenimiento; el Empleado ve solo los de sus equipos.
export default function CalendarPage() {
  const { role } = useAuth();
  const canManage = role === "Administrador" || role === "Técnico";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [range, setRange] = useState<{ from: string; to: string } | null>(null);
  const [selected, setSelected] = useState<CalendarEvent | null>(null);

  const { data: events, error } = useQuery({
    queryKey: ["calendar", range?.from, range?.to],
    queryFn: () => getCalendar(range!.from, range!.to),
    enabled: !!range,
  });
  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["calendar"] });
    queryClient.invalidateQueries({ queryKey: ["plans"] });
  };
  const reschedule = useMutation({
    mutationFn: ({ planId, date }: { planId: string; date: string }) => reschedulePlan(planId, date),
    onSettled: refresh,
  });
  const start = useMutation({
    mutationFn: (planId: string) => startPlan(planId),
    onSuccess: () => {
      refresh();
      setSelected(null);
      navigate("/mantenimientos");
    },
  });

  function onDatesSet(arg: DatesSetArg) {
    const iso = (d: Date) => d.toISOString().slice(0, 10);
    setRange({ from: iso(arg.start), to: iso(arg.end) });
  }

  function onEventDrop(arg: EventDropArg) {
    const ev = arg.event.extendedProps as CalendarEvent;
    if (!ev.planId || !arg.event.startStr) return arg.revert();
    reschedule.mutate({ planId: ev.planId, date: arg.event.startStr.slice(0, 10) }, { onError: () => arg.revert() });
  }

  function onEventClick(arg: EventClickArg) {
    setSelected(arg.event.extendedProps as CalendarEvent);
  }

  const actionError = reschedule.error ?? start.error ?? error;

  return (
    <div className="space-y-4 p-6">
      <div>
        <h1 className="text-xl font-semibold">Calendario de mantenimiento</h1>
        <p className="text-sm text-gray-400">
          Planes preventivos cada N semanas, mantenimientos en curso y vencimientos de contratos.
          {canManage && " Arrastra un mantenimiento programado para cambiar su fecha."}
        </p>
      </div>

      <div className="flex flex-wrap gap-4 text-xs">
        {LEGEND.map(([key, label, color]) => (
          <span key={key} className="flex items-center gap-1">
            <span className="inline-block h-3 w-3 rounded" style={{ background: color }} /> {label}
          </span>
        ))}
      </div>

      {actionError && <p className="text-sm text-red-500">{getApiErrorMessage(actionError)}</p>}

      <div className="rounded-lg bg-white p-3 text-gray-800">
        <FullCalendar
          plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
          initialView="dayGridMonth"
          locale={esLocale}
          headerToolbar={{ left: "prev,next today", center: "title", right: "dayGridMonth,timeGridWeek" }}
          height="auto"
          datesSet={onDatesSet}
          editable={canManage}
          eventDrop={onEventDrop}
          eventClick={onEventClick}
          events={(events ?? []).map((e) => ({
            id: e.id,
            title: e.title,
            start: e.date,
            allDay: true,
            editable: canManage && e.editable,
            backgroundColor: e.overdue ? "#dc2626" : COLORS[e.kind],
            borderColor: e.overdue ? "#dc2626" : COLORS[e.kind],
            classNames: e.projected ? ["opacity-60"] : [],
            extendedProps: e,
          }))}
        />
      </div>

      {selected && (
        <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/50" onClick={() => setSelected(null)}>
          <div className="w-96 space-y-3 rounded-lg bg-[#111b30] p-4 text-sm" onClick={(e) => e.stopPropagation()}>
            <h2 className="font-semibold">{selected.title}</h2>
            <p className="text-gray-400">
              {selected.date}
              {selected.technician ? ` · Responsable: ${selected.technician}` : ""}
              {selected.overdue ? " · Vencido" : ""}
              {selected.projected ? " · Fecha proyectada según la periodicidad" : ""}
            </p>
            <div className="flex flex-wrap gap-2">
              {selected.kind === "plan" && canManage && !selected.projected && (
                <button disabled={start.isPending} onClick={() => start.mutate(selected.planId!)}
                  className="rounded bg-blue-600 px-3 py-1 text-white disabled:opacity-50">
                  Iniciar mantenimiento
                </button>
              )}
              {selected.equipmentId && (
                <button onClick={() => navigate(`/equipos/${selected.equipmentId}`)} className="rounded border border-white/20 px-3 py-1">
                  Ver equipo
                </button>
              )}
              {selected.kind === "contract" && (
                <button onClick={() => navigate("/alquiler")} className="rounded border border-white/20 px-3 py-1">
                  Ver contratos
                </button>
              )}
              <button onClick={() => setSelected(null)} className="px-3 py-1 text-gray-400">Cerrar</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
