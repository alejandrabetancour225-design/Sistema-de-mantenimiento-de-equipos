import { useMemo, useState } from "react";
import { useAllMaintenances } from "../hooks/useMaintenance";
import {
  MAINTENANCE_TYPE_LABEL,
  MAINTENANCE_STATUS_LABEL,
} from "../types/maintenance";

// Solo lectura. Es la única pantalla a la que puede entrar un Cliente
// (el backend le permite únicamente consultar mantenimientos).
export default function MaintenanceHistoryPage() {
  const { data, isLoading, error } = useAllMaintenances();
  const [search, setSearch] = useState("");
  const [openId, setOpenId] = useState<string | null>(null);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return (data ?? []).filter(
      (m) =>
        !term ||
        (m.equipmentInternalCode ?? "").toLowerCase().includes(term) ||
        m.reportedProblem.toLowerCase().includes(term)
    );
  }, [data, search]);

  if (isLoading) return <p className="p-8">Cargando mantenimientos...</p>;
  if (error) return <p className="p-8 text-red-600">Error al cargar los mantenimientos.</p>;

  return (
    <div className="p-8 space-y-4">
      <h1 className="text-xl font-semibold">Historial de mantenimientos</h1>

      <input
        placeholder="Buscar por código de equipo o problema..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="input max-w-sm"
      />

      <ul className="space-y-2 max-w-3xl">
        {filtered.map((m) => (
          <li key={m.id} className="border rounded p-3 text-sm space-y-2">
            <div className="flex justify-between items-start gap-4">
              <div>
                <p className="font-medium">
                  {m.equipmentInternalCode} — {MAINTENANCE_TYPE_LABEL[m.type]}
                </p>
                <p>{m.reportedProblem}</p>
                <p className="text-xs text-gray-500">
                  {MAINTENANCE_STATUS_LABEL[m.status]} ·{" "}
                  {new Date(m.startedAt).toLocaleDateString()} · Total: $
                  {m.totalCost.toFixed(2)}
                </p>
              </div>
              <button
                className="text-blue-600 text-xs shrink-0"
                onClick={() => setOpenId(openId === m.id ? null : m.id)}
              >
                {openId === m.id ? "Ocultar" : "Detalle"}
              </button>
            </div>

            {openId === m.id && (
              <div className="border-t pt-2 space-y-1 text-xs">
                <p>
                  <span className="font-medium">Técnico:</span> {m.technicianName ?? "—"}
                </p>
                <p>
                  <span className="font-medium">Trabajo realizado:</span>{" "}
                  {m.workDone || "—"}
                </p>
                <p>
                  <span className="font-medium">Costos:</span> mano de obra $
                  {m.laborCost.toFixed(2)} · repuestos ${m.sparePartsCost.toFixed(2)} ·
                  otros ${m.otherCosts.toFixed(2)}
                </p>
                <p className="font-medium">Repuestos usados</p>
                <ul className="list-disc pl-5">
                  {m.spareParts.map((sp) => (
                    <li key={sp.id}>
                      {sp.sparePartName} x{sp.quantity} — ${sp.subtotal.toFixed(2)}
                    </li>
                  ))}
                  {m.spareParts.length === 0 && <li>Ninguno</li>}
                </ul>
              </div>
            )}
          </li>
        ))}
        {filtered.length === 0 && (
          <li className="text-sm text-gray-500">No hay mantenimientos registrados.</li>
        )}
      </ul>
    </div>
  );
}