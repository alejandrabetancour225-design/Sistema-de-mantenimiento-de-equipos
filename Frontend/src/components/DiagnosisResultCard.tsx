import { useState } from "react";
import { AlertTriangle, Brain, MessageSquare, Wrench } from "lucide-react";
import type { AIDiagnosis } from "../types/diagnosis";
import { useAuth } from "../context/authContext";
import FeedbackModal from "./FeedbackModal";

interface Props {
  diagnosis: AIDiagnosis;
  compact?: boolean;
  onFeedbackSubmitted?: () => void;
}

export default function DiagnosisResultCard({ diagnosis, compact = false, onFeedbackSubmitted }: Props) {
  const { role } = useAuth();
  const [showFeedback, setShowFeedback] = useState(false);
  const canGiveFeedback =
    (role === "Administrador" || role === "Técnico") && diagnosis.status === "PENDING";

  return (
    <div className="rounded-xl border border-white/10 bg-[#0f1729] p-6 space-y-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-semibold text-white flex items-center gap-2">
            <Brain size={18} />
            Diagnóstico · {diagnosis.equipmentInternalCode}
          </h3>
          <p className="text-sm text-gray-400">
            {diagnosis.symptom} · {new Date(diagnosis.createdAt).toLocaleString()}
          </p>
          <p className="text-xs text-gray-500 mt-1">
            Por {diagnosis.createdByName}
          </p>
        </div>
        <span
          className="rounded-full bg-blue-500/10 px-3 py-1 text-xs font-medium text-blue-400 shrink-0"
          title="Probabilidad de la causa más probable"
        >
          {Math.round(diagnosis.confidence * 100)}% · mejor apuesta
        </span>
      </div>

      <div className="space-y-3">
        {diagnosis.rankedCauses.map((cause, index) => (
          <div
            key={index}
            className="rounded-lg border border-white/10 bg-white/5 p-4"
          >
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-white">
                {index + 1}. {cause.cause}
              </span>
              <span className="text-xs font-medium text-blue-400">
                {Math.round(cause.probability * 100)}%
              </span>
            </div>
            <p className="text-sm text-gray-300">
              <span className="font-medium text-gray-400">Recomendación:</span>{" "}
              {cause.recommendation}
            </p>
          </div>
        ))}
      </div>

      {!compact && (
        <>
          <div className="flex items-start gap-2 rounded-lg border border-yellow-500/20 bg-yellow-500/5 p-3">
            <AlertTriangle size={18} className="text-yellow-400 mt-0.5 shrink-0" />
            <div>
              <p className="text-xs font-medium text-yellow-400">
                Razonamiento de la IA
              </p>
              <p className="text-sm text-gray-300">{diagnosis.reasoning}</p>
            </div>
          </div>

          {diagnosis.requiresTechnician && (
            <div className="flex items-center gap-2 rounded-lg border border-red-500/20 bg-red-500/5 p-3">
              <Wrench size={18} className="text-red-400" />
              <p className="text-sm text-red-300">
                Este problema requiere intervención de un técnico.
              </p>
            </div>
          )}

          {diagnosis.contextSnapshot && (
            <details className="rounded-lg border border-white/10 bg-white/5 p-3">
              <summary className="text-xs font-medium text-gray-400 cursor-pointer">
                Contexto usado en el diagnóstico (HU-20)
              </summary>
              <div className="mt-2 space-y-1 text-xs text-gray-400">
                <p>
                  <span className="font-medium">Marca:</span>{" "}
                  {diagnosis.contextSnapshot.brand}
                </p>
                <p>
                  <span className="font-medium">Modelo:</span>{" "}
                  {diagnosis.contextSnapshot.model}
                </p>
                <p>
                  <span className="font-medium">Sistema operativo:</span>{" "}
                  {diagnosis.contextSnapshot.osInfo}
                </p>
                <p>
                  <span className="font-medium">Componentes:</span>{" "}
                  {diagnosis.contextSnapshot.components.length > 0
                    ? diagnosis.contextSnapshot.components.join(", ")
                    : "dato no disponible"}
                </p>
              </div>
            </details>
          )}

          <div className="flex items-center justify-between">
            <p className="text-xs text-gray-500">
              Modelo: {diagnosis.modelVersion} · Estado: {diagnosis.status}
            </p>

            {canGiveFeedback && (
              <button
                onClick={() => setShowFeedback(true)}
                className="flex items-center gap-2 rounded-lg bg-green-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-green-700"
              >
                <MessageSquare size={14} />
                Confirmar causa real
              </button>
            )}

            {diagnosis.status !== "PENDING" && (
              <span
                className={`rounded-full px-2 py-1 text-xs font-medium ${
                  diagnosis.status === "CONFIRMED"
                    ? "bg-green-100 text-green-800"
                    : "bg-orange-100 text-orange-800"
                }`}
              >
                {diagnosis.status === "CONFIRMED" ? "IA acertó" : "Corregido"}
              </span>
            )}
          </div>
        </>
      )}

      {showFeedback && (
        <FeedbackModal
          diagnosis={diagnosis}
          onClose={() => setShowFeedback(false)}
          onSuccess={() => {
            setShowFeedback(false);
            onFeedbackSubmitted?.();
          }}
        />
      )}
    </div>
  );
}
