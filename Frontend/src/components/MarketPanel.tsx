import { useMutation } from "@tanstack/react-query";
import { Globe, Loader2 } from "lucide-react";
import { api } from "../services/api";
import { getApiErrorMessage } from "../lib/errors";

interface MarketResult {
  title: string;
  url: string;
  snippet: string;
  query: string;
  retrieved_at: string;
}

// Precio de reemplazo y guías de mantenimiento del modelo del equipo (buscadores gratuitos).
export default function MarketPanel({ equipmentId }: { equipmentId: string }) {
  const search = useMutation({
    mutationFn: async () =>
      (await api.get<{ results: MarketResult[]; available: boolean; retrievedAt: string }>(`/market/Equipment/${equipmentId}`, { timeout: 100_000 })).data,
  });
  return (
    <section className="space-y-2 text-sm">
      <div className="flex items-center justify-between">
        <h2 className="font-medium">Mercado y guías</h2>
        <button onClick={() => search.mutate()} disabled={search.isPending} className="flex items-center gap-1 text-xs text-blue-500 disabled:opacity-50">
          {search.isPending ? <Loader2 size={14} className="animate-spin" /> : <Globe size={14} />} Buscar precio y guías
        </button>
      </div>
      {search.error && <p className="text-xs text-red-500">{getApiErrorMessage(search.error)}</p>}
      {search.data && !search.data.available && <p className="text-xs text-gray-400">Los buscadores no respondieron; intenta más tarde.</p>}
      {search.data?.results.length ? (
        <ul className="space-y-2">
          {search.data.results.map((r) => (
            <li key={r.url} className="rounded border p-2">
              <a href={r.url} target="_blank" rel="noreferrer noopener" className="font-medium text-blue-500 underline">{r.title}</a>
              <p className="text-xs text-gray-500">{r.snippet}</p>
              <p className="text-[10px] text-gray-400">Búsqueda: "{r.query}" · consultado {new Date(r.retrieved_at).toLocaleString()}</p>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
