import { AlertTriangle, Clock, Package, Wrench } from "lucide-react";
import { useAuth } from "../context/authContext";
import {
  useEquipmentSummary,
  useOpenIncidents,
  useUpcomingMaintenances,
} from "../hooks/useDashboard";
import DashboardCard from "../components/DashboardCard";
import DashboardChart from "../components/DashboardChart";

export default function DashboardPage() {
  const { fullName, role } = useAuth();
  const summary = useEquipmentSummary();
  const incidents = useOpenIncidents();
  const maintenances = useUpcomingMaintenances();

  if (summary.isLoading || incidents.isLoading || maintenances.isLoading) {
    return <p className="p-8 text-gray-400">Cargando dashboard...</p>;
  }

  if (summary.error || incidents.error || maintenances.error) {
    return <p className="p-8 text-red-500">Error al cargar el dashboard.</p>;
  }

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold text-white">Dashboard</h1>
        <p className="text-sm text-gray-400">
          Bienvenido, {fullName} · <span className="uppercase">{role}</span>
        </p>
      </div>

      {/* Tarjetas resumen */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <DashboardCard
          title="Total de equipos"
          value={summary.data?.total ?? 0}
          icon={<Package size={20} />}
          accent="blue"
        />
        <DashboardCard
          title="Disponibles"
          value={summary.data?.available ?? 0}
          icon={<Package size={20} />}
          accent="green"
        />
        <DashboardCard
          title="En mantenimiento"
          value={summary.data?.underMaintenance ?? 0}
          icon={<Wrench size={20} />}
          accent="yellow"
        />
        <DashboardCard
          title="Incidencias abiertas"
          value={incidents.data?.length ?? 0}
          icon={<AlertTriangle size={20} />}
          accent="red"
        />
      </div>

      {/* Gráfico de equipos por estado */}
      {summary.data && <DashboardChart data={summary.data} />}

      {/* Incidencias abiertas */}
      <div className="rounded-xl border border-white/10 bg-[#0f1729] p-6">
        <div className="mb-4 flex items-center gap-2">
          <AlertTriangle size={20} className="text-red-400" />
          <h2 className="text-lg font-semibold text-white">Incidencias abiertas</h2>
        </div>
        {incidents.data && incidents.data.length > 0 ? (
          <div className="space-y-2">
            {incidents.data.map((incident) => (
              <div
                key={incident.id}
                className={`flex items-center justify-between rounded-lg border p-3 ${
                  incident.isUrgent
                    ? "border-red-500/40 bg-red-500/5"
                    : "border-white/10 bg-white/5"
                }`}
              >
                <div className="flex-1">
                  <p className="text-sm font-medium text-white">
                    {incident.equipmentInternalCode}
                  </p>
                  <p className="text-xs text-gray-400">{incident.description}</p>
                </div>
                <div className="text-right">
                  <p
                    className={`text-xs font-medium ${
                      incident.isUrgent ? "text-red-400" : "text-gray-400"
                    }`}
                  >
                    {incident.daysOpen} día(s)
                  </p>
                  <p className="text-xs text-gray-500">{incident.status}</p>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No hay incidencias abiertas.</p>
        )}
      </div>

      {/* Próximos mantenimientos */}
      <div className="rounded-xl border border-white/10 bg-[#0f1729] p-6">
        <div className="mb-4 flex items-center gap-2">
          <Clock size={20} className="text-yellow-400" />
          <h2 className="text-lg font-semibold text-white">Próximos mantenimientos</h2>
        </div>
        {maintenances.data && maintenances.data.length > 0 ? (
          <div className="space-y-2">
            {maintenances.data.map((m) => (
              <div
                key={m.id}
                className={`flex items-center justify-between rounded-lg border p-3 ${
                  m.isOverdue
                    ? "border-red-500/40 bg-red-500/5"
                    : m.daysUntil <= 7
                    ? "border-yellow-500/40 bg-yellow-500/5"
                    : "border-white/10 bg-white/5"
                }`}
              >
                <div className="flex-1">
                  <p className="text-sm font-medium text-white">
                    {m.equipmentInternalCode}
                  </p>
                  <p className="text-xs text-gray-400">
                    Técnico: {m.technicianName}
                  </p>
                </div>
                <div className="text-right">
                  <p
                    className={`text-xs font-medium ${
                      m.isOverdue
                        ? "text-red-400"
                        : m.daysUntil <= 7
                        ? "text-yellow-400"
                        : "text-gray-400"
                    }`}
                  >
                    {m.isOverdue
                      ? `Vencido hace ${Math.abs(m.daysUntil)} día(s)`
                      : `En ${m.daysUntil} día(s)`}
                  </p>
                  <p className="text-xs text-gray-500">{m.nextMaintenanceDate}</p>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No hay mantenimientos próximos.</p>
        )}
      </div>
    </div>
  );
}
