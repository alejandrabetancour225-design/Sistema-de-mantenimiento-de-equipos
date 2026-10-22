import { SEVERITY_COLOR, SEVERITY_LABEL, type ErrorSeverity } from "../types/errorKnowledge";

export default function SeverityBadge({ severity }: { severity: ErrorSeverity }) {
  return (
    <span className={`rounded-full px-2 py-1 text-xs font-medium ${SEVERITY_COLOR[severity]}`}>
      {SEVERITY_LABEL[severity]}
    </span>
  );
}
