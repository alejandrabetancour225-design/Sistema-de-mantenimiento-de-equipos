import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Bot, CheckCircle2, Loader2, RotateCcw, Send, ShieldCheck, User } from "lucide-react";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { triage, type ChatMessage, type TriageResponse } from "../services/assistantServices";
import { createIncident } from "../services/incidentServices";
import { getApiErrorMessage } from "../lib/errors";

const URGENCY_LABEL: Record<TriageResponse["urgency"], string> = {
  baja: "Urgencia baja",
  media: "Urgencia media",
  alta: "Urgencia alta",
};

// Reportar una falla con palabras propias ("se fundió", "se pega"...).
// El asistente entiende, pregunta lo necesario (máx. 3 veces) y da consejos seguros.
// R5: la incidencia solo se crea cuando el empleado lo confirma.
export default function AssistantPage() {
  const queryClient = useQueryClient();
  const { data: equipments, isLoading } = useEquipmentList();
  const myEquipment = (equipments ?? []).filter((e) => e.status !== "DECOMMISSIONED");

  const [equipmentId, setEquipmentId] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [last, setLast] = useState<TriageResponse | null>(null);
  const [created, setCreated] = useState(false);

  const triageMutation = useMutation({
    mutationFn: (history: ChatMessage[]) => triage(equipmentId, history),
    onSuccess: (res) => {
      setLast(res);
      if (res.needsMoreInfo && res.question) {
        setMessages((prev) => [...prev, { role: "asistente", content: res.question! }]);
      }
    },
  });

  const incidentMutation = useMutation({
    mutationFn: () => {
      const words = messages.filter((m) => m.role === "empleado").map((m) => m.content).join(" / ");
      const description =
        `Reporte del empleado: ${words}\n\nResumen del asistente: ${last?.technicalSummary ?? ""}`.slice(0, 2000);
      return createIncident({ equipmentId, description });
    },
    onSuccess: () => {
      setCreated(true);
      queryClient.invalidateQueries({ queryKey: ["incidents"] });
      queryClient.invalidateQueries({ queryKey: ["equipment-incidents"] });
    },
  });

  function send(e: React.FormEvent) {
    e.preventDefault();
    const text = draft.trim();
    if (!text || !equipmentId) return;
    const history: ChatMessage[] = [...messages, { role: "empleado", content: text }];
    setMessages(history);
    setDraft("");
    triageMutation.mutate(history);
  }

  function restart() {
    setMessages([]);
    setLast(null);
    setCreated(false);
    setDraft("");
    triageMutation.reset();
    incidentMutation.reset();
  }

  const finished = !!last && !last.needsMoreInfo;
  const waiting = triageMutation.isPending;

  return (
    <div className="mx-auto max-w-2xl space-y-4 p-6">
      <div>
        <h1 className="text-xl font-semibold">Reportar una falla</h1>
        <p className="text-sm text-gray-400">
          Cuéntenos con sus propias palabras qué le pasa al equipo. El asistente le hará unas preguntas si
          las necesita y le dará consejos seguros mientras llega el técnico.
        </p>
      </div>

      <label className="block text-sm">
        <span className="font-medium">Equipo</span>
        <select
          value={equipmentId}
          onChange={(e) => {
            setEquipmentId(e.target.value);
            restart();
          }}
          className="input mt-1 w-full"
          disabled={isLoading || messages.length > 0}
        >
          <option value="">{isLoading ? "Cargando..." : "Seleccione su equipo"}</option>
          {myEquipment.map((eq) => (
            <option key={eq.id} value={eq.id}>
              {eq.internalCode} · {eq.type} {eq.brand} {eq.model}
            </option>
          ))}
        </select>
        {!isLoading && myEquipment.length === 0 && (
          <span className="mt-1 block text-xs text-gray-400">No tiene equipos asignados.</span>
        )}
      </label>

      <div className="space-y-3 rounded-lg border border-white/10 p-4">
        {messages.length === 0 && (
          <p className="text-sm text-gray-400">
            Ejemplo: <em>"el pc se fundió y no prende"</em> o <em>"se pega a cada rato y echa candela"</em>.
          </p>
        )}
        {messages.map((m, i) => (
          <div key={i} className={`flex gap-2 ${m.role === "empleado" ? "justify-end" : ""}`}>
            {m.role === "asistente" && <Bot size={18} className="mt-1 shrink-0 text-blue-400" />}
            <p
              className={`max-w-[80%] rounded-lg px-3 py-2 text-sm ${
                m.role === "empleado" ? "bg-blue-600 text-white" : "bg-white/5"
              }`}
            >
              {m.content}
            </p>
            {m.role === "empleado" && <User size={18} className="mt-1 shrink-0 text-gray-400" />}
          </div>
        ))}
        {waiting && (
          <p className="flex items-center gap-2 text-sm text-gray-400">
            <Loader2 size={16} className="animate-spin" /> El asistente está analizando… (puede tardar uno o dos
            minutos)
          </p>
        )}
        {triageMutation.error && (
          <p className="text-sm text-red-500">{getApiErrorMessage(triageMutation.error)}</p>
        )}
      </div>

      {last && (
        <div className="space-y-2 rounded-lg border border-white/10 p-4 text-sm">
          <p>
            <span className="font-medium">Lo que entendí:</span> {last.understanding}
          </p>
          {last.safeTips.length > 0 && (
            <div>
              <p className="flex items-center gap-1 font-medium">
                <ShieldCheck size={16} className="text-green-500" /> Mientras tanto, puede intentar:
              </p>
              <ul className="ml-6 list-disc">
                {last.safeTips.map((t) => (
                  <li key={t}>{t}</li>
                ))}
              </ul>
            </div>
          )}
          {finished && <p className="text-xs text-gray-400">{URGENCY_LABEL[last.urgency]}</p>}
        </div>
      )}

      {!finished && !created && (
        <form onSubmit={send} className="flex gap-2">
          <input
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            placeholder={messages.length ? "Escriba su respuesta…" : "¿Qué le pasa al equipo?"}
            className="input flex-1"
            maxLength={1000}
            disabled={!equipmentId || waiting}
          />
          <button
            type="submit"
            disabled={!equipmentId || !draft.trim() || waiting}
            className="flex items-center gap-1 rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
          >
            <Send size={16} /> Enviar
          </button>
        </form>
      )}

      {finished && !created && (
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => incidentMutation.mutate()}
            disabled={incidentMutation.isPending}
            className="rounded bg-green-600 px-4 py-2 text-white disabled:opacity-50"
          >
            {incidentMutation.isPending ? "Enviando…" : "Enviar reporte al técnico"}
          </button>
          <button onClick={restart} className="flex items-center gap-1 px-3 py-2 text-gray-400">
            <RotateCcw size={16} /> Empezar de nuevo
          </button>
        </div>
      )}
      {incidentMutation.error && (
        <p className="text-sm text-red-500">{getApiErrorMessage(incidentMutation.error)}</p>
      )}
      {created && (
        <p className="flex items-center gap-2 rounded bg-green-500/10 p-3 text-sm text-green-500">
          <CheckCircle2 size={18} /> Su reporte quedó registrado. Un técnico lo revisará pronto.
        </p>
      )}
    </div>
  );
}
