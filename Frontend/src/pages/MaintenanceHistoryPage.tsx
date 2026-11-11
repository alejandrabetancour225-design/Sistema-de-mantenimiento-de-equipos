import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAllMaintenances, useSpareParts } from "../hooks/useMaintenance";
import { useUsers } from "../hooks/useUsers";
import { useAuth } from "../context/authContext";
import MaintenanceCreateForm from "../components/MaintenanceCreateForm";
import MaintenanceDetail from "../components/MaintenanceDetail";
import { money } from "../lib/maintenanceUtils";
import {
  MAINTENANCE_TYPE_LABEL,
  MAINTENANCE_STATUS_LABEL,
  type MaintenanceStatus,
} from "../types/maintenance";

// Pantalla única de mantenimientos.
// - Administrador y Técnico: registran, editan, cierran y gestionan repuestos aquí.
// - Empleado y Cliente (gerente): solo consultan.
export default function MaintenanceHistoryPage() {
  const { role } = useAuth();
  const isAdmin = role === "Administrador";
  const canManage = isAdmin || role === "Técnico";

  const { data, isLoading, error } = useAllMaintenances();
  const { data: spareParts } = useSpareParts();
  const { data: users } = useUsers(isAdmin);
  const technicians = (users ?? []).filter(
    (u) => u.active && (u.role === "Técnico" || u.role === "Administrador")
  );

  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<MaintenanceStatus | "">("");
  const [showForm, setShowForm] = useState(false);
  const [openId, setOpenId] = useState<string | null>(null);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (data ?? [])
      .filter(
        (m) =>
          (!term ||
            (m.equipmentInternalCode ?? "").toLowerCase().includes(term) ||
            m.reportedProblem.toLowerCase().includes(term)) &&
          (!statusFilter || m.status === statusFilter)
      )
      // Más reciente primero
      .sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime());
  }, [data, search, statusFilter]);

  // Resumen por equipo (sobre lo que se está viendo con los filtros)
  const byEquipment = useMemo(() => {
    const map = new Map<
      string,
      { equipmentId: string; code: string; count: number; labor: number; parts: number; other: number; total: number }
    >();
    for (const m of filtered) {
      const row = map.get(m.equipmentId) ?? {
        equipmentId: m.equipmentId,
        code: m.equipmentInternalCode ?? m.equipmentId.slice(0, 8),
        count: 0,
        labor: 0,
        parts: 0,
        other: 0,
        total: 0,
      };
      row.count += 1;
      row.labor += m.laborCost;
      row.parts += m.sparePartsCost;
      row.other += m.otherCosts;
      row.total += m.totalCost;
      map.set(m.equipmentId, row);
    }
    return [...map.values()].sort((a, b) => b.total - a.total);
  }, [filtered]);

  const totals = byEquipment.reduce(
    (acc, r) => ({
      count: acc.count + r.count,
      labor: acc.labor + r.labor,
      parts: acc.parts + r.parts,
      other: acc.other + r.other,
      total: acc.total + r.total,
    }),
    { count: 0, labor: 0, parts: 0, other: 0, total: 0 }
  );

  if (isLoading) return <p className="p-8">Cargando mantenimientos...</p>;
  if (error) return <p className="p-8 text-red-600">Error al cargar los mantenimientos.</p>;

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold">Mantenimientos</h1>
          <p className="text-sm text-gray-600">
            {canManage
              ? "Registra y gestiona los mantenimientos de todos los equipos desde aquí."
              : "Consulta de los mantenimientos de todos los equipos, del más reciente al más antiguo."}
          </p>
        </div>
        {canManage && (
          <button
            onClick={() => setShowForm((v) => !v)}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white"
          >
            {showForm ? "Cerrar formulario" : "+ Nuevo mantenimiento"}
          </button>
        )}
      </div>

      {canManage && showForm && (
        <div className="max-w-3xl">
          <MaintenanceCreateForm onCreated={() => setShowForm(false)} />
        </div>
      )}

      <div className="flex flex-wrap gap-3">
        <input
          placeholder="Buscar por código de equipo o problema..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="input max-w-sm"
        />
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as MaintenanceStatus | "")}
          className="input w-48"
        >
          <option value="">Todos los estados</option>
          {Object.entries(MAINTENANCE_STATUS_LABEL).map(([v, l]) => (
            <option key={v} value={v}>
              {l}
            </option>
          ))}
        </select>
      </div>

      <section className="space-y-2">
        <h2 className="font-medium">Costo acumulado por equipo</h2>
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="py-2">Equipo</th>
                <th>Mant.</th>
                <th>Mano de obra</th>
                <th>Repuestos</th>
                <th>Otros</th>
                <th>Total</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {byEquipment.map((r) => (
                <tr key={r.equipmentId} className="border-b">
                  <td className="py-2 font-medium">{r.code}</td>
                  <td>{r.count}</td>
                  <td>{money(r.labor)}</td>
                  <td>{money(r.parts)}</td>
                  <td>{money(r.other)}</td>
                  <td className="font-semibold">{money(r.total)}</td>
                  <td className="text-right">
                    <Link to={`/equipos/${r.equipmentId}/historial`} className="text-blue-600">
                      Ver historial completo
                    </Link>
                  </td>
                </tr>
              ))}
              {byEquipment.length > 0 && (
                <tr className="bg-gray-50 font-medium">
                  <td className="py-2">Total general</td>
                  <td>{totals.count}</td>
                  <td>{money(totals.labor)}</td>
                  <td>{money(totals.parts)}</td>
                  <td>{money(totals.other)}</td>
                  <td>{money(totals.total)}</td>
                  <td></td>
                </tr>
              )}
              {byEquipment.length === 0 && (
                <tr>
                  <td colSpan={7} className="py-3 text-gray-500">
                    No hay mantenimientos registrados.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="space-y-2">
        <h2 className="font-medium">Detalle por fecha</h2>
        <ul className="max-w-3xl space-y-2">
          {filtered.map((m) => (
            <li key={m.id} className="space-y-2 rounded border p-3 text-sm">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="font-medium">
                    {m.equipmentInternalCode} — {MAINTENANCE_TYPE_LABEL[m.type]}
                  </p>
                  <p>{m.reportedProblem}</p>
                  <p className="text-xs text-gray-500">
                    {MAINTENANCE_STATUS_LABEL[m.status]} · {new Date(m.startedAt).toLocaleDateString()} ·
                    Técnico: {m.technicianName ?? "—"} · Total: {money(m.totalCost)}
                  </p>
                </div>
                <button
                  className="shrink-0 text-xs text-blue-600"
                  onClick={() => setOpenId(openId === m.id ? null : m.id)}
                >
                  {openId === m.id ? "Ocultar" : canManage ? "Gestionar" : "Detalle"}
                </button>
              </div>

              {openId === m.id &&
                (canManage ? (
                  <MaintenanceDetail
                    maintenance={m}
                    isAdmin={isAdmin}
                    technicians={technicians}
                    availableSpareParts={spareParts ?? []}
                  />
                ) : (
                  <div className="space-y-1 border-t pt-2 text-xs">
                    <p>
                      <span className="font-medium">Trabajo realizado:</span> {m.workDone || "—"}
                    </p>
                    <p>
                      <span className="font-medium">Costos:</span> mano de obra {money(m.laborCost)} ·
                      repuestos {money(m.sparePartsCost)} · otros {money(m.otherCosts)}
                    </p>
                    <p className="font-medium">Repuestos usados</p>
                    <ul className="list-disc pl-5">
                      {m.spareParts.map((sp) => (
                        <li key={sp.id}>
                          {sp.sparePartName} x{sp.quantity} — {money(sp.subtotal)}
                        </li>
                      ))}
                      {m.spareParts.length === 0 && <li>Ninguno</li>}
                    </ul>
                  </div>
                ))}
            </li>
          ))}
          {filtered.length === 0 && (
            <li className="text-sm text-gray-500">No hay mantenimientos con esos filtros.</li>
          )}
        </ul>
      </section>
    </div>
  );
}
