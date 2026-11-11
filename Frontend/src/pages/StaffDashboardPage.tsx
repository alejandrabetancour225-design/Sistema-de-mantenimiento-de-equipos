import { useState } from "react";
import { AlertTriangle, Clock, Package, Wrench } from "lucide-react";
import { useAuth } from "../context/authContext";
import DashboardCard from "../components/DashboardCard";
import DashboardChart from "../components/DashboardChart";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { useAllMaintenances } from "../hooks/useMaintenance";
import { useAssignments } from "../hooks/useAssignments";
import type { DashboardEquipmentSummary } from "../types/dashboard";

const RANGES = [7, 15, 30, 60, 90];

// Técnico y Empleado: el backend no les abre /dashboard, así que se arma con los datos
// que sí pueden consultar (equipos, mantenimientos y asignaciones).
export default function StaffDashboardPage() {
  const { fullName, role, userId } = useAuth();
  const isEmployee = role === "Empleado";
  const equipment = useEquipmentList();
  const maintenances = useAllMaintenances();
  const assignments = useAssignments(isEmployee);
  const [daysAhead, setDaysAhead] = useState(30);

  if (equipment.isLoading || maintenances.isLoading) {
    return <p className="p-8 text-gray-500">Cargando dashboard...</p>;
  }
  if (equipment.error || maintenances.error) {
    return <p className="p-8 text-red-600">Error al cargar el dashboard.</p>;
  }

  const eq = equipment.data ?? [];
  const count = (st: string) => eq.filter((e) => e.status === st).length;
  const summary: DashboardEquipmentSummary = {
    total: eq.length,
    available: count("AVAILABLE"),
    inUse: count("IN_USE"),
    underMaintenance: count("UNDER_MAINTENANCE"),
    outOfService: count("OUT_OF_SERVICE"),
    decommissioned: count("DECOMMISSIONED"),
  };

  const all = maintenances.data ?? [];
  const open = all.filter((m) => m.status === "OPEN" || m.status === "IN_PROGRESS");
  const myOpen = open.filter((m) => m.technicianId === userId);

  // Próximos mantenimientos: los que tienen fecha programada y no están cancelados
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const upcoming = all
    .filter((m) => m.nextMaintenanceDate && m.status !== "CANCELLED")
    .map((m) => {
      const date = new Date(`${m.nextMaintenanceDate}T00:00:00`);
      const daysUntil = Math.round((date.getTime() - today.getTime()) / 86400000);
      return { m, daysUntil };
    })
    .filter((x) => x.daysUntil <= daysAhead)
    .sort((a, b) => a.daysUntil - b.daysUntil);

  const myEquipment = (assignments.data ?? []).filter((a) => a.status === "ACTIVE");

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-sm text-gray-600">
          Bienvenido, {fullName} · <span className="uppercase">{role}</span>
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <DashboardCard title="Total de equipos" value={summary.total} icon={<Package size={20} />} accent="blue" />
        <DashboardCard title="Disponibles" value={summary.available} icon={<Package size={20} />} accent="green" />
        <DashboardCard title="En mantenimiento" value={summary.underMaintenance} icon={<Wrench size={20} />} accent="yellow" />
        {isEmployee ? (
          <DashboardCard title="Mis equipos" value={myEquipment.length} icon={<Package size={20} />} accent="gray" />
        ) : (
          <DashboardCard
            title="Mis mantenimientos abiertos"
            value={myOpen.length}
            icon={<AlertTriangle size={20} />}
            accent="red"
          />
        )}
      </div>

      <DashboardChart data={summary} />

      {isEmployee && (
        <div className="rounded-xl border p-6">
          <h2 className="mb-3 text-lg font-semibold">Mis equipos asignados</h2>
          {myEquipment.length > 0 ? (
            <ul className="space-y-2 text-sm">
              {myEquipment.map((a) => (
                <li key={a.id} className="rounded-lg border bg-gray-50 p-3">
                  <span className="font-medium">{a.equipmentInternalCode}</span>
                  <span className="text-xs text-gray-500">
                    {" "}
                    · asignado el {new Date(a.assignedAt).toLocaleDateString()}
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-gray-500">No tienes equipos asignados.</p>
          )}
        </div>
      )}

      <div className="rounded-xl border p-6">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Clock size={20} className="text-yellow-600" />
            <h2 className="text-lg font-semibold">Próximos mantenimientos</h2>
          </div>
          <label className="text-xs text-gray-600">
            Ventana:{" "}
            <select
              value={daysAhead}
              onChange={(e) => setDaysAhead(Number(e.target.value))}
              className="rounded border px-2 py-1"
            >
              {RANGES.map((d) => (
                <option key={d} value={d}>
                  {d} días
                </option>
              ))}
            </select>
          </label>
        </div>
        {upcoming.length > 0 ? (
          <div className="space-y-2">
            {upcoming.map(({ m, daysUntil }) => (
              <div
                key={m.id}
                className={`flex items-center justify-between rounded-lg border p-3 ${
                  daysUntil < 0
                    ? "border-red-300 bg-red-50"
                    : daysUntil <= 7
                    ? "border-yellow-300 bg-yellow-50"
                    : "bg-gray-50"
                }`}
              >
                <div className="flex-1">
                  <p className="text-sm font-medium">{m.equipmentInternalCode}</p>
                  <p className="text-xs text-gray-600">Técnico: {m.technicianName ?? "—"}</p>
                </div>
                <div className="text-right">
                  <p className={`text-xs font-medium ${daysUntil < 0 ? "text-red-600" : "text-gray-600"}`}>
                    {daysUntil < 0
                      ? `Vencido hace ${Math.abs(daysUntil)} día(s)`
                      : daysUntil === 0
                      ? "Hoy"
                      : `En ${daysUntil} día(s)`}
                  </p>
                  <p className="text-xs text-gray-500">{m.nextMaintenanceDate}</p>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No hay mantenimientos próximos en {daysAhead} días.</p>
        )}
      </div>
    </div>
  );
}