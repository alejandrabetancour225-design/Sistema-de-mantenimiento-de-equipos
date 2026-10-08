import type { ReactNode } from "react";

interface DashboardCardProps {
  title: string;
  value: number | string;
  icon?: ReactNode;
  accent?: "blue" | "green" | "yellow" | "red" | "gray";
}

const ACCENTS = {
  blue: "border-blue-500/30 bg-blue-500/5",
  green: "border-green-500/30 bg-green-500/5",
  yellow: "border-yellow-500/30 bg-yellow-500/5",
  red: "border-red-500/30 bg-red-500/5",
  gray: "border-gray-500/30 bg-gray-500/5",
};

export default function DashboardCard({
  title,
  value,
  icon,
  accent = "blue",
}: DashboardCardProps) {
  return (
    <div className={`rounded-xl border p-5 ${ACCENTS[accent]}`}>
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-gray-400">{title}</p>
        {icon && <div className="text-gray-400">{icon}</div>}
      </div>
      <p className="mt-2 text-3xl font-semibold text-white">{value}</p>
    </div>
  );
}
