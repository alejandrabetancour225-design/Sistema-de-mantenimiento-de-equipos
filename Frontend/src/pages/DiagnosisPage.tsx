import { useState } from "react";
import { Brain, Loader2 } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { useCreateDiagnosis, useRecentDiagnoses } from "../hooks/useDiagnosis";
import DiagnosisResultCard from "../components/DiagnosisResultCard";
import { getApiErrorMessage } from "../lib/errors";
import type { AIDiagnosis } from "../types/diagnosis";

export default function DiagnosisPage() {
  const { data: equipments, isLoading: loadingEquipments } = useEquipmentList();
  const { data: recent, isLoading: loadingRecent } = useRecentDiagnoses();
  const createMutation = useCreateDiagnosis();
  const queryClient = useQueryClient();

  const [equipmentId, setEquipmentId] = useState("");
  const [symptom, setSymptom] = useState("");
  const [error, setError] = useState("");
  const [result, setResult] = useState<AIDiagnosis | null>(null);

  function refresh() {
    queryClient.invalidateQueries({ queryKey: ["diagnoses"] });
    queryClient.invalidateQueries({ queryKey: ["feedbacks"] });
    queryClient.invalidateQueries({ queryKey: ["ai", "metrics"] });
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    setResult(null);

    if (!equipmentId) {
      setError("Selecciona un equipo.");
      return;
    }
    if (symptom.trim().length < 3) {
      setError("Describe el síntoma (mínimo 3 caracteres).");
      return;
    }

    try {
      const diagnosis = await createMutation.mutateAsync({
        equipmentId,
        symptom: symptom.trim(),
      });
      setResult(diagnosis);
      setSymptom("");
    } catch (err) {
      setError(getApiErrorMessage(err, "No se pudo generar el diagnóstico."));
    }
  }

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold text-white flex items-center gap-2">
          <Brain size={24} /> Diagnóstico Asistido por IA
        </h1>
        <p className="text-sm text-gray-400">
          Describe el síntoma y la IA te dará las causas más probables.
        </p>
      </div>

      <form
        onSubmit={handleSubmit}
        className="rounded-xl border border-white/10 bg-[#0f1729] p-6 space-y-4"
      >
        <div>
          <label className="block text-xs text-gray-400 mb-1">Equipo</label>
          <select
            value={equipmentId}
            onChange={(e) => setEquipmentId(e.target.value)}
            disabled={loadingEquipments || createMutation.isPending}
            className="w-full rounded-lg border border-white/10 bg-[#111527] px-3 py-2 text-sm text-white"
          >
            <option value="">Selecciona un equipo...</option>
            {equipments?.map((eq) => (
              <option key={eq.id} value={eq.id}>
                {eq.internalCode} · {eq.brand} {eq.model}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-xs text-gray-400 mb-1">Síntoma</label>
          <textarea
            value={symptom}
            onChange={(e) => setSymptom(e.target.value)}
            disabled={createMutation.isPending}
            placeholder="Ej: El equipo no enciende, la pantalla parpadea..."
            rows={3}
            className="w-full rounded-lg border border-white/10 bg-[#111527] px-3 py-2 text-sm text-white"
          />
        </div>

        {error && <p className="text-sm text-red-400">{error}</p>}

        <button
          type="submit"
          disabled={createMutation.isPending}
          className="flex items-center gap-2 rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {createMutation.isPending ? (
            <>
              <Loader2 size={16} className="animate-spin" />
              Analizando... (puede tardar 20-40s)
            </>
          ) : (
            <>
              <Brain size={16} />
              Diagnosticar
            </>
          )}
        </button>
      </form>

      {result && (
        <DiagnosisResultCard diagnosis={result} onFeedbackSubmitted={refresh} />
      )}

      <div>
        <h2 className="text-lg font-semibold text-white mb-3">Diagnósticos recientes</h2>
        {loadingRecent ? (
          <p className="text-gray-400">Cargando...</p>
        ) : recent && recent.length > 0 ? (
          <div className="grid gap-4 md:grid-cols-2">
            {recent.map((d) => (
              <DiagnosisResultCard
                key={d.id}
                diagnosis={d}
                onFeedbackSubmitted={refresh}
              />
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No hay diagnósticos aún.</p>
        )}
      </div>
    </div>
  );
}
