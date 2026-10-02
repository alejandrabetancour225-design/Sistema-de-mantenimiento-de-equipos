import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSpareParts } from "../hooks/useMaintenance";
import {
  createSparePart,
  updateSparePart,
  deactivateSparePart,
  reactivateSparePart,
} from "../services/sparePartServices";

export default function SparePartsPage() {
  const { data, isLoading, error } = useSpareParts(true);
  const queryClient = useQueryClient();

  const [form, setForm] = useState({ name: "", description: "", unitCost: 0 });

  const createMutation = useMutation({
    mutationFn: createSparePart,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["spareparts"] });
      setForm({ name: "", description: "", unitCost: 0 });
    },
  });

  const costMutation = useMutation({
    mutationFn: ({ id, unitCost }: { id: string; unitCost: number }) =>
      updateSparePart(id, { unitCost }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["spareparts"] }),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active ? reactivateSparePart(id) : deactivateSparePart(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["spareparts"] }),
  });

  const createError = (createMutation.error as any)?.response?.data?.message;

  function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) return;
    createMutation.mutate(form);
  }

  if (isLoading) return <p className="p-8">Cargando repuestos...</p>;
  if (error) return <p className="p-8 text-red-600">Error al cargar los repuestos.</p>;

  return (
    <div className="p-8 space-y-6">
      <h1 className="text-xl font-semibold">Repuestos</h1>

      <form onSubmit={handleCreate} className="flex flex-wrap gap-2 text-sm max-w-xl">
        <input
          placeholder="Nombre"
          value={form.name}
          onChange={(e) => setForm({ ...form, name: e.target.value })}
          className="input flex-1 min-w-[140px]"
        />
        <input
          placeholder="Descripción (opcional)"
          value={form.description}
          onChange={(e) => setForm({ ...form, description: e.target.value })}
          className="input flex-1 min-w-[140px]"
        />
        <input
          type="number"
          step="0.01"
          placeholder="Costo unitario"
          value={form.unitCost}
          onChange={(e) => setForm({ ...form, unitCost: Number(e.target.value) })}
          className="input w-32"
        />
        <button
          type="submit"
          disabled={createMutation.isPending}
          className="bg-blue-600 text-white px-3 py-2 rounded disabled:opacity-50"
        >
          Agregar
        </button>
      </form>
      {createError && <p className="text-sm text-red-600">{createError}</p>}

      <table className="w-full border-collapse text-sm max-w-2xl">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2">Nombre</th>
            <th>Descripción</th>
            <th>Costo unitario</th>
            <th>Estado</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {data?.map((sp) => (
            <tr key={sp.id} className="border-b">
              <td className="py-2">{sp.name}</td>
              <td>{sp.description || "—"}</td>
              <td>
                <input
                  type="number"
                  step="0.01"
                  defaultValue={sp.unitCost}
                  onBlur={(e) =>
                    costMutation.mutate({ id: sp.id, unitCost: Number(e.target.value) })
                  }
                  className="w-24 rounded border px-1 py-0.5"
                />
              </td>
              <td>{sp.active ? "Activo" : "Inactivo"}</td>
              <td>
                <button
                  onClick={() => toggleMutation.mutate({ id: sp.id, active: !sp.active })}
                  className={sp.active ? "text-red-600" : "text-green-600"}
                >
                  {sp.active ? "Desactivar" : "Reactivar"}
                </button>
              </td>
            </tr>
          ))}
          {data?.length === 0 && (
            <tr>
              <td colSpan={5} className="py-4 text-center text-gray-500">
                No hay repuestos registrados.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}