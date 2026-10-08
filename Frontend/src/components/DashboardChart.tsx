import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts";
import type { DashboardEquipmentSummary } from "../types/dashboard";

const STATUS_LABELS: Record<string, string> = {
  available: "Disponible",
  inUse: "En uso",
  underMaintenance: "En mantenimiento",
  outOfService: "Fuera de servicio",
  decommissioned: "Dado de baja",
};

const STATUS_COLORS: Record<string, string> = {
  available: "#10b981",
  inUse: "#3b82f6",
  underMaintenance: "#eab308",
  outOfService: "#f97316",
  decommissioned: "#6b7280",
};

interface DashboardChartProps {
  data: DashboardEquipmentSummary;
}

export default function DashboardChart({ data }: DashboardChartProps) {
  const chartData = [
    { key: "available", label: STATUS_LABELS.available, value: data.available, color: STATUS_COLORS.available },
    { key: "inUse", label: STATUS_LABELS.inUse, value: data.inUse, color: STATUS_COLORS.inUse },
    { key: "underMaintenance", label: STATUS_LABELS.underMaintenance, value: data.underMaintenance, color: STATUS_COLORS.underMaintenance },
    { key: "outOfService", label: STATUS_LABELS.outOfService, value: data.outOfService, color: STATUS_COLORS.outOfService },
    { key: "decommissioned", label: STATUS_LABELS.decommissioned, value: data.decommissioned, color: STATUS_COLORS.decommissioned },
  ];

  return (
    <div className="rounded-xl border border-white/10 bg-[#0f1729] p-6">
      <h2 className="mb-4 text-lg font-semibold text-white">Equipos por estado</h2>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={chartData}>
          <CartesianGrid strokeDasharray="3 3" stroke="#ffffff10" />
          <XAxis dataKey="label" tick={{ fill: "#9ca3af", fontSize: 12 }} />
          <YAxis tick={{ fill: "#9ca3af", fontSize: 12 }} allowDecimals={false} />
          <Tooltip
            contentStyle={{
              backgroundColor: "#111527",
              border: "1px solid #ffffff20",
              borderRadius: "8px",
              color: "#fff",
            }}
          />
          <Bar dataKey="value" radius={[4, 4, 0, 0]}>
            {chartData.map((entry, index) => (
              <Cell key={index} fill={entry.color} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
