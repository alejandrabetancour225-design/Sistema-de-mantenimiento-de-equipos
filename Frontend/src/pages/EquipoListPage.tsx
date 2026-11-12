import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { deleteEquipment } from "../services/equipmentServices";
import { EQUIPMENT_STATUS_LABEL, type EquipmentStatus } from "../types/equipment";
import { useAuth } from "../context/authContext";
import EquipmentStatusMenu from "../components/EquipmentStatusMenu";
import { getApiErrorMessage } from "../lib/errors";
import { manualStatusActions } from "../lib/equipmentTransitions";

// Solo Administrador y Técnico pueden crear/editar/dar de baja equipos
// (mismo criterio que el backend en los endpoints Post/Put/Delete).
const CAN_MANAGE_EQUIPMENT = ["Administrador", "Técnico"];

export function EquipoListPage() {
  const { role } = useAuth();
  const canManage = !!role && CAN_MANAGE_EQUIPMENT.includes(role);
  const { data, isLoading, error } = useEquipmentList();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<EquipmentStatus | "">("");
  const [typeFilter, setTypeFilter] = useState("");
  const [locationFilter, setLocationFilter] = useState("");

  const distinct = (values: string[]) =>
    [...new Set(values.filter(Boolean))].sort((a, b) => a.localeCompare(b, "es"));
  const types = useMemo(() => distinct((data ?? []).map((e) => e.type)), [data]);
  const locations = useMemo(() => distinct((data ?? []).map((e) => e.location)), [data]);

  const deleteMutation = useMutation({
    mutationFn: deleteEquipment,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["equipment"] }),
  });

  // El endpoint no soporta filtros por query params todavía (Sprint 1),
  // así que se filtra en cliente mientras se agregan en el backend.
  const filtered = useMemo(() => {
    if (!data) return [];
    const q = search.trim().toLowerCase();
    return data.filter((eq) => {
      const matchesSearch =
        !q ||
        eq.internalCode.toLowerCase().includes(q) ||
        eq.serialNumber.toLowerCase().includes(q) ||
        eq.brand.toLowerCase().includes(q) ||
        eq.model.toLowerCase().includes(q);
      const matchesStatus = !statusFilter || eq.status === statusFilter;
      const matchesType = !typeFilter || eq.type === typeFilter;
      const matchesLocation = !locationFilter || eq.location === locationFilter;
      return matchesSearch && matchesStatus && matchesType && matchesLocation;
    });
  }, [data, search, statusFilter, typeFilter, locationFilter]);

  function handleDelete(id: string) {
    if (!confirm("Dar de baja un equipo es definitivo: no podrá volver a servicio. ¿Continuar?")) return;
    deleteMutation.mutate(id);
  }

  if (isLoading) return <p>Cargando equipos...</p>;
  if (error) return <p className="text-red-600">Error al cargar los equipos.</p>;

  return (
    <div className="space-y-4 px-6 py-6">
     <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">Equipos</h1>

        {canManage && (
          <Link
              to="/equipos/nuevo"
              className="rounded-lg bg-blue-600 px-5 py-2.5 font-medium text-white shadow-sm transition hover:bg-blue-700"
          >
              + Nuevo equipo
          </Link>
        )}
       </div>
      <div className="flex flex-wrap gap-2">
        <input
          placeholder="Buscar por código, serie, marca o modelo..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="rounded border px-3 py-2"
        />
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as EquipmentStatus | "")}
          className="rounded border px-3 py-2"
        >
          <option value="">Todos los estados</option>
          {Object.entries(EQUIPMENT_STATUS_LABEL).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
        <select
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
          className="rounded border px-3 py-2"
        >
          <option value="">Todos los tipos</option>
          {types.map((t) => (
            <option key={t} value={t}>
              {t}
            </option>
          ))}
        </select>
        <select
          value={locationFilter}
          onChange={(e) => setLocationFilter(e.target.value)}
          className="rounded border px-3 py-2"
        >
          <option value="">Todas las ubicaciones</option>
          {locations.map((l) => (
            <option key={l} value={l}>
              {l}
            </option>
          ))}
        </select>
      </div>

      {deleteMutation.error && (
        <p className="text-sm text-red-600">{getApiErrorMessage(deleteMutation.error)}</p>
      )}

      {canManage && (
        <p className="text-xs text-gray-500">
          Para cambiar el estado de un equipo, haz clic sobre su estado.
        </p>
      )}

      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2">Código</th>
            <th>Tipo</th>
            <th>Marca / Modelo</th>
            <th>Ubicación</th>
            <th>Estado</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((eq) => (
            <tr key={eq.id} className="border-b">
              <td className="py-2 font-medium">{eq.internalCode}</td>
              <td>{eq.type}</td>
              <td>{eq.brand} {eq.model}</td>
              <td>{eq.location}</td>
              <td>
                <EquipmentStatusMenu equipment={eq} canChange={canManage} />
              </td>
              <td className="space-x-2 text-right">
                {canManage && (
                  <>
                    <Link to={`/equipos/${eq.id}/editar`} className="text-blue-600">
                      Editar
                    </Link>
                    {manualStatusActions(eq.status, role).some((a) => a.kind === "decommission") && (
                      <button onClick={() => handleDelete(eq.id)} className="text-red-600">
                        Dar de baja
                      </button>
                    )}
                  </>
                )}
              </td>
            </tr>
          ))}
          {filtered.length === 0 && (
            <tr>
              <td colSpan={6} className="py-4 text-center text-gray-500">
                No hay equipos que coincidan.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}