import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useEquipmentHistory } from "../hooks/useMaintenance";
import { getEquipmentById } from "../services/equipmentServices";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";
import { MAINTENANCE_STATUS_LABEL, MAINTENANCE_TYPE_LABEL } from "../types/maintenance";
import { INCIDENT_STATUS_LABEL } from "../types/incident";

const money = (n: number) => `$${n.toFixed(2)}`;

type TimelineItem =
  | { kind: "maintenance"; date: string; id: string; data: NonNullable<ReturnType<typeof useEquipmentHistory>["data"]>["maintenances"][number] }
  | { kind: "incident"; date: string; id: string; data: NonNullable<ReturnType<typeof useEquipmentHistory>["data"]>["incidents"][number] };

// Trazabilidad completa de un equipo: mantenimientos + incidencias en orden cronológico.
// Disponible para todos los roles (el backend permite el historial a cualquier usuario).
export default function EquipmentHistoryPage() {
  const { id } = useParams<{ id: string }>();
  const equipmentId = id!;
  const { role } = useAuth();
  const isStaff = role !== "Cliente";

  const { data: history, isLoading, error } = useEquipmentHistory(equipmentId);
  // El Cliente no puede consultar equipos: solo el personal interno ve los datos de adquisición
  const { data: equipment } = useQuery({
    queryKey: ["equipment", equipmentId],
    queryFn: () => getEquipmentById(equipmentId),
    enabled: isStaff,
  });

  if (isLoading) return <p className="p-8">Cargando historial...</p>;
  if (error) {
    return <p className="p-8 text-red-600">{getApiErrorMessage(error, "No se pudo cargar el historial.")}</p>;
  }
  if (!history) return null;

  const items: TimelineItem[] = [
    ...history.maintenances.map((m) => ({
      kind: "maintenance" as const,
      date: m.startedAt,
      id: m.id,
      data: m,
    })),
    ...(history.incidents ?? []).map((i) => ({
      kind: "incident" as const,
      date: i.reportedAt,
      id: i.id,
      data: i,
    })),
  ].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

  const completed = history.maintenances.filter((m) => m.status === "COMPLETED").length;
  const price = equipment?.acquisitionPrice ?? null;
  const ratio = price && price > 0 ? (history.totalCost / price) * 100 : null;

  return (
    <div className="max-w-3xl space-y-6 p-8">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <Link to={isStaff ? `/equipos/${equipmentId}` : "/mantenimientos"} className="text-sm text-blue-600">
            ← {isStaff ? "Volver al equipo" : "Volver a mantenimientos"}
          </Link>
          <h1 className="mt-2 text-xl font-semibold">
            Historial de {history.equipmentInternalCode ?? "equipo"}
          </h1>
          {equipment && (
            <p className="text-sm text-gray-600">
              {equipment.type} · {equipment.brand} {equipment.model}
            </p>
          )}
        </div>
        <button onClick={() => window.print()} className="rounded border px-3 py-1.5 text-sm print:hidden">
          Imprimir
        </button>
      </div>

      <section className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-4">
        <Box label="Mantenimientos" value={`${history.maintenances.length} (${completed} completados)`} />
        <Box label="Incidencias" value={String(history.incidents?.length ?? 0)} />
        <Box label="Mano de obra" value={money(history.totalLaborCost)} />
        <Box label="Repuestos" value={money(history.totalSparePartsCost)} />
        <Box label="Otros costos" value={money(history.totalOtherCosts)} />
        <Box label="Costo total acumulado" value={money(history.totalCost)} strong />
        {price != null && <Box label="Precio de adquisición" value={money(price)} />}
        {ratio != null && (
          <Box
            label="Costo vs. adquisición"
            value={`${ratio.toFixed(0)}%`}
            warn={ratio >= 50}
          />
        )}
      </section>
      {ratio != null && ratio >= 50 && (
        <p className="rounded border border-yellow-300 bg-yellow-50 p-3 text-sm">
          El costo acumulado de mantenimiento ya supera la mitad del precio de adquisición. Conviene
          evaluar si es mejor reparar o reemplazar el equipo.
        </p>
      )}

      <section className="space-y-2">
        <h2 className="font-medium">Línea de tiempo</h2>
        <ol className="space-y-2">
          {items.map((it) => (
            <li
              key={`${it.kind}-${it.id}`}
              className={`space-y-1 rounded border p-3 text-sm ${
                it.kind === "incident" ? "border-orange-200 bg-orange-50" : ""
              }`}
            >
              {it.kind === "maintenance" ? (
                <>
                  <p className="font-medium">
                    Mantenimiento {MAINTENANCE_TYPE_LABEL[it.data.type].toLowerCase()} —{" "}
                    {MAINTENANCE_STATUS_LABEL[it.data.status]}
                  </p>
                  <p className="text-xs text-gray-500">
                    {new Date(it.date).toLocaleDateString()} · Técnico: {it.data.technicianName ?? "—"}
                  </p>
                  <p>{it.data.reportedProblem}</p>
                  {it.data.workDone && (
                    <p className="text-xs">
                      <span className="font-medium">Trabajo realizado:</span> {it.data.workDone}
                    </p>
                  )}
                  {it.data.spareParts.length > 0 && (
                    <p className="text-xs">
                      <span className="font-medium">Repuestos:</span>{" "}
                      {it.data.spareParts.map((sp) => `${sp.sparePartName} x${sp.quantity}`).join(", ")}
                    </p>
                  )}
                  <p className="text-xs">
                    Costo: {money(it.data.totalCost)}
                    {it.data.observations && ` · ${it.data.observations}`}
                  </p>
                </>
              ) : (
                <>
                  <p className="font-medium">Incidencia — {INCIDENT_STATUS_LABEL[it.data.status]}</p>
                  <p className="text-xs text-gray-500">
                    {new Date(it.date).toLocaleDateString()} · Reportó: {it.data.reportedByName ?? "—"}
                  </p>
                  <p>{it.data.description}</p>
                </>
              )}
            </li>
          ))}
          {items.length === 0 && (
            <li className="text-sm text-gray-500">Este equipo aún no tiene mantenimientos ni incidencias.</li>
          )}
        </ol>
      </section>
    </div>
  );
}

function Box({ label, value, strong, warn }: { label: string; value: string; strong?: boolean; warn?: boolean }) {
  return (
    <div
      className={`rounded border p-2 ${strong ? "border-blue-300 bg-blue-50" : ""} ${
        warn ? "border-yellow-300 bg-yellow-50" : ""
      }`}
    >
      <p className="text-xs text-gray-500">{label}</p>
      <p className={strong ? "font-semibold" : ""}>{value}</p>
    </div>
  );
}
