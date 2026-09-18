import { EQUIPMENT_STATUS_LABEL, type EquipmentStatus } from "../types/equipment";

const COLORS: Record<EquipmentStatus, string> = {
  AVAILABLE: "bg-green-100 text-green-800",
  IN_USE: "bg-blue-100 text-blue-800",
  UNDER_MAINTENANCE: "bg-yellow-100 text-yellow-800",
  OUT_OF_SERVICE: "bg-orange-100 text-orange-800",
  DECOMMISSIONED: "bg-gray-200 text-gray-600",
};

export default function StatusBadge({ status }: { status: EquipmentStatus }) {
  return (
    <span className={`rounded-full px-2 py-1 text-xs font-medium ${COLORS[status]}`}>
      {EQUIPMENT_STATUS_LABEL[status]}
    </span>
  );
}