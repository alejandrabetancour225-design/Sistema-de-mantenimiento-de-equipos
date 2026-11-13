import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileDown, Loader2, Sparkles } from "lucide-react";
import { api } from "../services/api";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

interface ReportRow {
  id: string;
  periodFrom: string;
  periodTo: string;
  status: "PENDING" | "DONE" | "FAILED";
  automatic: boolean;
  createdAt: string;
  error?: string | null;
  hasPdf: boolean;
}

interface ReportDetail extends ReportRow {
  content?: {
    summary: string;
    findings: string[];
    recommendations: { text: string; priority: string }[];
    alerts: string[];
    market_notes: { note: string; title: string; url: string; retrieved_at: string }[];
    model: string;
  } | null;
}

const STATUS_LABEL = { PENDING: "Generando…", DONE: "Listo", FAILED: "Falló" };

// Reportes ejecutivos redactados por IA: automáticos el día 1 de cada mes y bajo demanda.
// Incluyen información de mercado con fuentes y fecha. Plan Integraciones.
export default function AIReportsPage() {
  const { plan } = useAuth();
  const queryClient = useQueryClient();
  const [selected, setSelected] = useState<string | null>(null);

  const list = useQuery({
    queryKey: ["ai-reports"],
    queryFn: async () => (await api.get<ReportRow[]>("/aireports")).data,
    enabled: plan === "INTEGRACIONES",
    refetchInterval: (q) => (q.state.data?.some((r) => r.status === "PENDING") ? 10_000 : false),
  });
  const detail = useQuery({
    queryKey: ["ai-report", selected],
    queryFn: async () => (await api.get<ReportDetail>(`/aireports/${selected}`)).data,
    enabled: !!selected,
  });
  const generate = useMutation({
    mutationFn: async () => (await api.post<{ id: string }>("/aireports/Generate", {})).data,
    onSuccess: (r) => {
      queryClient.invalidateQueries({ queryKey: ["ai-reports"] });
      setSelected(r.id);
    },
  });

  async function downloadPdf(id: string, period: string) {
    const res = await api.get(`/aireports/${id}/Pdf`, { responseType: "blob" });
    const url = URL.createObjectURL(res.data as Blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `reporte-ejecutivo-${period}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  }

  if (plan !== "INTEGRACIONES") {
    return (
      <div className="mx-auto max-w-xl space-y-3 p-8">
        <h1 className="text-xl font-semibold">Reportes con IA</h1>
        <p className="text-sm text-gray-400">
          Los reportes ejecutivos generados por IA y la información de mercado están en el plan
          <strong> Integraciones</strong>. Pídale al administrador de la plataforma que lo active.
        </p>
      </div>
    );
  }

  const content = detail.data?.content;
  return (
    <div className="space-y-4 p-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="text-xl font-semibold">Reportes con IA</h1>
          <p className="text-sm text-gray-400">
            Resumen de la gestión de equipos con recomendaciones e información de mercado. Se genera solo el día 1 de
            cada mes; también puede pedirlo ahora (tarda unos minutos).
          </p>
        </div>
        <button onClick={() => generate.mutate()} disabled={generate.isPending}
          className="flex items-center gap-2 rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50">
          <Sparkles size={16} /> Generar reporte de los últimos 30 días
        </button>
      </div>
      {(generate.error || list.error) && <p className="text-sm text-red-500">{getApiErrorMessage(generate.error ?? list.error)}</p>}

      <div className="grid gap-4 md:grid-cols-[18rem_1fr]">
        <ul className="divide-y divide-white/5 rounded border border-white/10 text-sm">
          {list.data?.map((r) => (
            <li key={r.id}>
              <button onClick={() => setSelected(r.id)}
                className={`block w-full px-3 py-2 text-left hover:bg-white/5 ${selected === r.id ? "bg-white/10" : ""}`}>
                <span className="block font-medium">{r.periodFrom} → {r.periodTo}</span>
                <span className="text-xs text-gray-400">
                  {STATUS_LABEL[r.status]} · {r.automatic ? "mensual" : "a pedido"}
                </span>
              </button>
            </li>
          ))}
          {list.data?.length === 0 && <li className="px-3 py-4 text-gray-400">Aún no hay reportes.</li>}
        </ul>

        <div className="space-y-3 rounded border border-white/10 p-4 text-sm">
          {!selected && <p className="text-gray-400">Elige un reporte.</p>}
          {detail.data?.status === "PENDING" && (
            <p className="flex items-center gap-2 text-gray-400"><Loader2 size={16} className="animate-spin" /> La IA está redactando el reporte…</p>
          )}
          {detail.data?.status === "FAILED" && <p className="text-red-500">No se pudo generar: {detail.data.error}</p>}
          {content && (
            <>
              <div className="flex items-center justify-between">
                <h2 className="font-semibold">Resumen</h2>
                {detail.data?.hasPdf !== false && (
                  <button onClick={() => downloadPdf(detail.data!.id, detail.data!.periodFrom)} className="flex items-center gap-1 text-xs text-blue-400">
                    <FileDown size={14} /> PDF
                  </button>
                )}
              </div>
              <p>{content.summary}</p>
              <h3 className="font-medium">Hallazgos</h3>
              <ul className="ml-5 list-disc">{content.findings.map((f) => <li key={f}>{f}</li>)}</ul>
              <h3 className="font-medium">Recomendaciones</h3>
              <ul className="ml-5 list-disc">
                {content.recommendations.map((r) => <li key={r.text}><span className="text-xs uppercase text-amber-500">{r.priority}</span> {r.text}</li>)}
              </ul>
              {content.alerts.length > 0 && (
                <>
                  <h3 className="font-medium">Alertas</h3>
                  <ul className="ml-5 list-disc text-red-400">{content.alerts.map((a) => <li key={a}>{a}</li>)}</ul>
                </>
              )}
              <h3 className="font-medium">Información de mercado</h3>
              {content.market_notes.length === 0 ? (
                <p className="text-gray-400">Sin información de mercado en este reporte.</p>
              ) : (
                <ul className="space-y-1">
                  {content.market_notes.map((m) => (
                    <li key={m.url}>
                      {m.note}{" "}
                      <a href={m.url} target="_blank" rel="noreferrer noopener" className="text-xs text-blue-400 underline">
                        {m.title}
                      </a>
                      <span className="text-xs text-gray-500"> · consultado {new Date(m.retrieved_at).toLocaleDateString()}</span>
                    </li>
                  ))}
                </ul>
              )}
              <p className="text-xs text-gray-500">Redactado por IA ({content.model}). Las decisiones son de la gerencia.</p>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
