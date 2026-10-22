import { useMemo, useState } from "react";
import { BookOpen, Pencil, Plus, Search, Trash2, Zap } from "lucide-react";
import {
  useDeleteErrorKnowledge,
  useErrorKnowledgeList,
} from "../hooks/useErrorKnowledge";
import SeverityBadge from "../components/SeverityBadge";
import ErrorKnowledgeModal from "../components/ErrorKnowledgeModal";
import type { ErrorKnowledge } from "../types/errorKnowledge";

export default function ErrorKnowledgePage() {
  const [search, setSearch] = useState("");
  const [componentFilter, setComponentFilter] = useState("");
  const { data, isLoading } = useErrorKnowledgeList();
  const deleteMutation = useDeleteErrorKnowledge();

  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ErrorKnowledge | undefined>(undefined);

  const filtered = useMemo(() => {
    if (!data) return [];
    return data.filter((k) => {
      const matchesSearch =
        !search ||
        k.symptom.toLowerCase().includes(search.toLowerCase()) ||
        k.possibleCauses.toLowerCase().includes(search.toLowerCase()) ||
        k.suggestedSolution.toLowerCase().includes(search.toLowerCase());
      const matchesComponent =
        !componentFilter ||
        k.componentType.toLowerCase().includes(componentFilter.toLowerCase());
      return matchesSearch && matchesComponent;
    });
  }, [data, search, componentFilter]);

  function handleEdit(entry: ErrorKnowledge) {
    setEditing(entry);
    setModalOpen(true);
  }

  function handleCreate() {
    setEditing(undefined);
    setModalOpen(true);
  }

  function handleDelete(id: string) {
    if (!confirm("¿Eliminar esta entrada de la biblioteca? (soft-delete)")) return;
    deleteMutation.mutate(id);
  }

  return (
    <div className="space-y-4 p-8">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold text-white flex items-center gap-2">
            <BookOpen size={24} /> Biblioteca de Errores (HU-17)
          </h1>
          <p className="text-sm text-gray-400">
            Conocimiento que alimenta a la IA en cada diagnóstico.
          </p>
        </div>
        <button
          onClick={handleCreate}
          className="flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          <Plus size={16} />
          Nueva entrada
        </button>
      </div>

      <div className="flex flex-wrap gap-2">
        <div className="relative flex-1 min-w-[200px]">
          <Search
            size={16}
            className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500"
          />
          <input
            placeholder="Buscar por síntoma, causa o solución..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-lg border border-white/10 bg-[#111527] pl-9 pr-3 py-2 text-sm text-white"
          />
        </div>
        <input
          placeholder="Filtrar por componente..."
          value={componentFilter}
          onChange={(e) => setComponentFilter(e.target.value)}
          className="rounded-lg border border-white/10 bg-[#111527] px-3 py-2 text-sm text-white"
        />
      </div>

      {isLoading ? (
        <p className="text-gray-400">Cargando...</p>
      ) : filtered.length > 0 ? (
        <div className="grid gap-4 md:grid-cols-2">
          {filtered.map((entry) => (
            <div
              key={entry.id}
              className="rounded-xl border border-white/10 bg-[#0f1729] p-5 space-y-2"
            >
              <div className="flex items-start justify-between gap-2">
                <div>
                  <h3 className="text-sm font-semibold text-white">{entry.symptom}</h3>
                  <p className="text-xs text-gray-500">
                    Componente: {entry.componentType}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <SeverityBadge severity={entry.severity} />
                  {entry.source === "Auto" && (
                    <span className="flex items-center gap-1 rounded-full bg-purple-500/10 px-2 py-1 text-xs font-medium text-purple-400">
                      <Zap size={10} />
                      Auto
                    </span>
                  )}
                </div>
              </div>

              <div>
                <p className="text-xs font-medium text-gray-400">Causas posibles:</p>
                <p className="text-sm text-gray-300">{entry.possibleCauses}</p>
              </div>

              <div>
                <p className="text-xs font-medium text-gray-400">Solución sugerida:</p>
                <p className="text-sm text-gray-300">{entry.suggestedSolution}</p>
              </div>

              <div className="flex items-center justify-between pt-2 border-t border-white/5">
                <p className="text-xs text-gray-500">
                  Confianza: {entry.confidence}%
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={() => handleEdit(entry)}
                    className="flex items-center gap-1 text-xs text-blue-400 hover:text-blue-300"
                  >
                    <Pencil size={12} /> Editar
                  </button>
                  <button
                    onClick={() => handleDelete(entry.id)}
                    disabled={deleteMutation.isPending}
                    className="flex items-center gap-1 text-xs text-red-400 hover:text-red-300 disabled:opacity-50"
                  >
                    <Trash2 size={12} /> Eliminar
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="rounded-xl border border-dashed border-white/10 p-8 text-center">
          <p className="text-sm text-gray-400">
            {data?.length === 0
              ? "La biblioteca está vacía. Añade la primera entrada."
              : "No hay resultados para los filtros aplicados."}
          </p>
        </div>
      )}

      {modalOpen && (
        <ErrorKnowledgeModal
          existing={editing}
          onClose={() => setModalOpen(false)}
          onSuccess={() => setModalOpen(false)}
        />
      )}
    </div>
  );
}
