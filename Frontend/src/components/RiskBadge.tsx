import type { RiskLevel } from "../types/prediction";

const COLORS: Record<RiskLevel, string> = {
  LOW: "bg-green-100 text-green-800",
  MEDIUM: "bg-yellow-100 text-yellow-800",
  HIGH: "bg-red-100 text-red-800",
};

const LABELS: Record<RiskLevel, string> = {
  LOW: "Riesgo bajo",
  MEDIUM: "Riesgo medio",
  HIGH: "Riesgo alto",
};

export default function RiskBadge({ level }: { level: RiskLevel }) {
  return (
    <span className={`rounded-full px-2 py-1 text-xs font-medium ${COLORS[level]}`}>
      {LABELS[level]}
    </span>
  );
}
