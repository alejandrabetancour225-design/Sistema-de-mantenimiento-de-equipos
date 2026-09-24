import type { Role } from "../types/auth";

const COLORS: Record<Role, string> = {
  Administrador: "bg-purple-100 text-purple-800",
  "Técnico": "bg-blue-100 text-blue-800",
  Empleado: "bg-green-100 text-green-800",
  Cliente: "bg-gray-200 text-gray-700",
};

export default function RoleBadge({ role }: { role: Role }) {
  return (
    <span className={`rounded-full px-2 py-1 text-xs font-medium ${COLORS[role]}`}>
      {role}
    </span>
  );
}
