import { useState } from "react";
import { Wrench } from "lucide-react";
import type { FailurePrediction } from "../types/prediction";
import { useCreatePreventiveMaintenance } from "../hooks/usePredictions";
import { getApiErrorMessage } from "../lib/errors";

interface Props {
  prediction: FailurePrediction;
  onClose: () => void;
  onSuccess: () => void;
}

export default function CreatePreventiveMaintenanceModal({ prediction, onClose, onSuccess }: Props) {
  const createMutation = useCreatePreventiveMaintenance();
  const [problem, setProblem] = useState(
    `Mantenimiento preventivo programado desde alerta de riesgo (score ${prediction.riskScore}). Componente en riesgo: ${prediction.riskComponent}.`
  );
  const [error, setError] = useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");

    if (problem.trim().length < 3) {
      setError("Describe el problema (mínimo 3 caracteres).");
      return;
    }

    try {
      await createMutation.mutateAsync({
        equipmentId: prediction.equipmentId,
        reportedProblem: problem.trim(),
      });
      onSuccess();
    } catch (err) {
      setError(getApiErrorMessage(err, "No se pudo crear el mantenimiento preventivo."));
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-md rounded-2xl border border-white/10 bg-[#111527] p-6 shadow-xl">
        <div className="flex items-center gap-2 mb-3">
          <Wrench size={20} className="text-yellow-400" />
          <h2 className="text-lg font-semibold text-white">
            Generar mantenimiento preventivo
          </h2>
        </div>

        <p className="text-sm text-gray-400 mb-1">
          Equipo: <span className="text-white font-medium">{prediction.equipmentInternalCode}</span>
        </p>
        <p className="text-sm text-gray-400">
          Riesgo: <span className="text-red-400 font-medium">{prediction.riskScore}/100</span> · {prediction.reason}
        </p>

        <form onSubmit={handleSubmit} className="mt-4 space-y-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">
              Descripción del problema
            </label>
            <textarea
              value={problem}
              onChange={(e) => setProblem(e.target.value)}
              rows={3}
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
              className="flex-1 rounded-lg bg-yellow-600 py-2 text-sm font-medium text-white hover:bg-yellow-700 disabled:opacity-50"
            >
              {createMutation.isPending ? "Creando..." : "Crear mantenimiento"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
