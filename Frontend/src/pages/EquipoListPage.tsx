import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEquipmentList } from "../hooks/useEquipmentList";
import { deleteEquipment } from "../services/equipmentServices";
import { EQUIPMENT_STATUS_LABEL, type EquipmentStatus } from "../types/equipment";

export function EquipoListPage() {
  const { data, isLoading, error } = useEquipmentList();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<EquipmentStatus | "">("");

  const deleteMutation = useMutation({
    mutationFn: deleteEquipment,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["equipment"] }),
  });

  // El endpoint no soporta filtros por query params todavía (Sprint 1),
  // así que se filtra en cliente mientras se agregan en el backend.
  const filtered = useMemo(() => {
    if (!data) return [];
    return data.filter((eq) => {
      const matchesSearch =
        !search ||
        eq.internalCode.toLowerCase().includes(search.toLowerCase()) ||
        eq.serialNumber.toLowerCase().includes(search.toLowerCase()) ||
        eq.brand.toLowerCase().includes(search.toLowerCase());
      const matchesStatus = !statusFilter || eq.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [data, search, statusFilter]);

  function handleDelete(id: string) {
    if (!confirm("¿Dar de baja este equipo?")) return;
    deleteMutation.mutate(id);
  }

  if (isLoading) return <p>Cargando equipos...</p>;
  if (error) return <p className="text-red-600">Error al cargar los equipos.</p>;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">Equipos</h1>
        <Link to="/equipos/nuevo" className="rounded bg-blue-600 px-4 py-2 text-white">
          + Nuevo equipo
        </Link>
      </div>

      <div className="flex gap-2">
        <input
          placeholder="Buscar por código, serie o marca..."
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
      </div>

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
              <td className="py-2">
                <Link to={`/equipos/${eq.id}`} className="text-blue-600 hover:underline">
                  {eq.internalCode}
                </Link>
              </td>
              <td>{eq.type}</td>
              <td>{eq.brand} {eq.model}</td>
              <td>{eq.location}</td>
              <td>{EQUIPMENT_STATUS_LABEL[eq.status]}</td>
              <td className="space-x-2 text-right">
                <Link to={`/equipos/${eq.id}/editar`} className="text-blue-600">
                  Editar
                </Link>
                <button onClick={() => handleDelete(eq.id)} className="text-red-600">
                  Dar de baja
                </button>
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