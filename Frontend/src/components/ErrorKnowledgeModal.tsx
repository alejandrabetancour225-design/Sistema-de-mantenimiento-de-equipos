import { useState } from "react";
import type {
  CreateErrorKnowledgeRequest,
  ErrorKnowledge,
  ErrorSeverity,
} from "../types/errorKnowledge";
import { SEVERITY_LABEL } from "../types/errorKnowledge";
import {
  useCreateErrorKnowledge,
  useUpdateErrorKnowledge,
} from "../hooks/useErrorKnowledge";
import { getApiErrorMessage } from "../lib/errors";

interface Props {
  existing?: ErrorKnowledge;
  onClose: () => void;
  onSuccess: () => void;
}

export default function ErrorKnowledgeModal({ existing, onClose, onSuccess }: Props) {
  const createMutation = useCreateErrorKnowledge();
  const updateMutation = useUpdateErrorKnowledge();
  const isEdit = !!existing;

  const [form, setForm] = useState<CreateErrorKnowledgeRequest>({
    symptom: existing?.symptom ?? "",
    componentType: existing?.componentType ?? "",
    possibleCauses: existing?.possibleCauses ?? "",
    suggestedSolution: existing?.suggestedSolution ?? "",
    severity: existing?.severity ?? "MEDIUM",
  });
  const [error, setError] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");

    if (
      !form.symptom.trim() ||
      !form.componentType.trim() ||
      !form.possibleCauses.trim() ||
      !form.suggestedSolution.trim()
    ) {
      setError("Todos los campos son obligatorios.");
      return;
    }

    try {
      if (isEdit) {
        await updateMutation.mutateAsync({ id: existing!.id, data: form });
      } else {
        await createMutation.mutateAsync(form);
      }
      onSuccess();
    } catch (err) {
      setError(getApiErrorMessage(err, "No se pudo guardar."));
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-lg rounded-2xl border border-white/10 bg-[#111527] p-6 shadow-xl">
        <h2 className="text-lg font-semibold text-white mb-4">
          {isEdit ? "Editar entrada" : "Nueva entrada en la biblioteca"}
        </h2>

        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Síntoma *</label>
            <input
              value={form.symptom}
              onChange={(e) => setForm({ ...form, symptom: e.target.value })}
              placeholder="Ej: Pantalla azul al iniciar"
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Tipo de componente *</label>
            <input
              value={form.componentType}
              onChange={(e) => setForm({ ...form, componentType: e.target.value })}
              placeholder="Ej: RAM, Disco Duro, Tarjeta Gráfica"
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Causas posibles *</label>
            <textarea
              value={form.possibleCauses}
              onChange={(e) => setForm({ ...form, possibleCauses: e.target.value })}
              rows={2}
              placeholder="Memoria defectuosa, contactos sucios..."
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Solución sugerida *</label>
            <textarea
              value={form.suggestedSolution}
              onChange={(e) => setForm({ ...form, suggestedSolution: e.target.value })}
              rows={2}
              placeholder="Limpiar contactos con borrador, probar con otra RAM..."
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            />
          </div>

          <div>
            <label className="block text-xs text-gray-400 mb-1">Severidad *</label>
            <select
              value={form.severity}
              onChange={(e) => setForm({ ...form, severity: e.target.value as ErrorSeverity })}
              className="w-full rounded-lg border border-white/10 bg-[#0f1729] px-3 py-2 text-sm text-white"
            >
              {(Object.keys(SEVERITY_LABEL) as ErrorSeverity[]).map((s) => (
                <option key={s} value={s}>
                  {SEVERITY_LABEL[s]}
                </option>
              ))}
            </select>
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
              disabled={isPending}
              className="flex-1 rounded-lg bg-blue-600 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {isPending ? "Guardando..." : isEdit ? "Guardar cambios" : "Crear"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
