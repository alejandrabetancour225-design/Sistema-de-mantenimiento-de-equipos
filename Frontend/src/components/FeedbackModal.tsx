import { useState } from "react";
import type { AIDiagnosis } from "../types/diagnosis";
import { useCreateFeedback } from "../hooks/useFeedback";
import { getApiErrorMessage } from "../lib/errors";

interface Props {
  diagnosis: AIDiagnosis;
  onClose: () => void;
  onSuccess: () => void;
}

export default function FeedbackModal({ diagnosis, onClose, onSuccess }: Props) {
  const createMutation = useCreateFeedback();
  const [selectedCause, setSelectedCause] = useState("");
  const [customCause, setCustomCause] = useState("");
  const [notes, setNotes] = useState("");
  const [error, setError] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");

    const predictedCause = diagnosis.rankedCauses[0]?.cause ?? "";
    const actualCause = selectedCause || customCause.trim();

    if (!actualCause) {
      setError("Selecciona la causa real o escribe una nueva.");
      return;
    }

    try {
      await createMutation.mutateAsync({
        diagnosisId: diagnosis.id,
        predictedCause,
        actualCause,
        notes: notes.trim() || undefined,
      });
      onSuccess();
    } catch (err) {
      setError(getApiErrorMessage(err, "No se pudo registrar el feedback."));
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-lg rounded-2xl border border-white/10 bg-[#111527] p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-white">
          Confirmar causa real
        </h2>
        <p className="mt-1 text-sm text-gray-400">
          Ayuda a la IA a mejorar. ¿Cuál fue la causa real del problema?
        </p>

        <div className="mt-4 rounded-lg border border-blue-500/20 bg-blue-500/5 p-3">
          <p className="text-xs font-medium text-blue-400">
            Causa predicha por la IA (más probable)
          </p>
          <p className="text-sm text-white mt-1">
            {diagnosis.rankedCauses[0]?.cause}
          </p>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 space-y-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">
              ¿Cuál fue la causa real?
            </label>
            <select
              value={selectedCause}
              onChange={(e) => {
                setSelectedCause(e.target.value);
                setCustomCause("");
              }}
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            >
              <option value="">Selecciona una de las causas de la IA...</option>
              {diagnosis.rankedCauses.map((c, i) => (
                <option key={i} value={c.cause}>
                  {c.cause}
                </option>
              ))}
              <option value="__custom__">Otra causa (escribir abajo)</option>
            </select>
          </div>

          {selectedCause === "__custom__" && (
            <div>
              <label className="block text-xs text-gray-400 mb-1">
                Escribe la causa real
              </label>
              <input
                type="text"
                value={customCause}
                onChange={(e) => setCustomCause(e.target.value)}
                placeholder="Ej: Cable de poder roto"
                className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
              />
            </div>
          )}

          <div>
            <label className="block text-xs text-gray-400 mb-1">
              Notas (opcional)
            </label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Detalles de la reparación..."
              rows={2}
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
          </div>

          {error && <p className="text-sm text-red-400">{error}</p>}

          <div className="flex gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-lg border border-white/10 py-2 text-sm text-gray-300 hover:bg-white/5"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="flex-1 rounded-lg bg-blue-600 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {createMutation.isPending ? "Guardando..." : "Confirmar"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
