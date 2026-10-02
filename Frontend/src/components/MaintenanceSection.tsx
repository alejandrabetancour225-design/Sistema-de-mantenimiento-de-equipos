import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useMaintenancesByEquipment, useSpareParts } from "../hooks/useMaintenance";
import { useUsers } from "../hooks/useUsers";
import {
  createMaintenance,
  closeMaintenance,
  addSparePart,
  removeSparePart,
  deleteMaintenance,
} from "../services/maintenanceServices";
import {
  MAINTENANCE_TYPE_LABEL,
  MAINTENANCE_STATUS_LABEL,
  type MaintenanceType,
} from "../types/maintenance";
import { useAuth } from "../context/authContext";

export default function MaintenanceSection({ equipmentId }: { equipmentId: string }) {
  const { role, userId } = useAuth();
  const isAdmin = role === "Administrador";
  const queryClient = useQueryClient();

  const { data: maintenances, isLoading } = useMaintenancesByEquipment(equipmentId);
  const { data: spareParts } = useSpareParts();
  // Solo Admin tiene acceso a /users; un Técnico se asigna a sí mismo.
  const { data: users } = useUsers();
  const technicians = (users ?? []).filter(
    (u) => u.role === "Técnico" || u.role === "Administrador"
  );

  const [openId, setOpenId] = useState<string | null>(null);

  const [form, setForm] = useState({
    technicianId: isAdmin ? "" : userId ?? "",
    type: "CORRECTIVE" as MaintenanceType,
    reportedProblem: "",
    laborCost: 0,
    otherCosts: 0,
    observations: "",
  });

  const createMutation = useMutation({
    mutationFn: createMaintenance,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenances", equipmentId] });
      setForm({
        technicianId: isAdmin ? "" : userId ?? "",
        type: "CORRECTIVE",
        reportedProblem: "",
        laborCost: 0,
        otherCosts: 0,
        observations: "",
      });
    },
  });

  function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!form.technicianId || !form.reportedProblem.trim()) return;
    createMutation.mutate({ equipmentId, ...form });
  }

  const createError = (createMutation.error as any)?.response?.data?.message;

  return (
    <section className="space-y-3">
      <h2 className="font-medium">Mantenimientos</h2>

      <form onSubmit={handleCreate} className="space-y-2 border rounded p-3 text-sm">
        <div className="flex flex-wrap gap-2">
          {isAdmin && (
            <select
              value={form.technicianId}
              onChange={(e) => setForm({ ...form, technicianId: e.target.value })}
              className="input flex-1 min-w-[160px]"
            >
              <option value="">Selecciona técnico</option>
              {technicians.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.fullName} ({t.role})
                </option>
              ))}
            </select>
          )}
          <select
            value={form.type}
            onChange={(e) => setForm({ ...form, type: e.target.value as MaintenanceType })}
            className="input flex-1 min-w-[140px]"
          >
            {Object.entries(MAINTENANCE_TYPE_LABEL).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        <input
          placeholder="Problema reportado"
          value={form.reportedProblem}
          onChange={(e) => setForm({ ...form, reportedProblem: e.target.value })}
          className="input w-full"
        />

        <div className="flex gap-2">
          <input
            type="number"
            step="0.01"
            placeholder="Costo mano de obra"
            value={form.laborCost}
            onChange={(e) => setForm({ ...form, laborCost: Number(e.target.value) })}
            className="input flex-1"
          />
          <input
            type="number"
            step="0.01"
            placeholder="Otros costos"
            value={form.otherCosts}
            onChange={(e) => setForm({ ...form, otherCosts: Number(e.target.value) })}
            className="input flex-1"
          />
        </div>

        {createError && <p className="text-red-600 text-xs">{createError}</p>}

        <button
          type="submit"
          disabled={createMutation.isPending}
          className="bg-blue-600 text-white px-3 py-2 rounded disabled:opacity-50"
        >
          Registrar mantenimiento
        </button>
      </form>

      {isLoading ? (
        <p className="text-sm text-gray-500">Cargando...</p>
      ) : (
        <ul className="space-y-2">
          {maintenances?.map((m) => (
            <li key={m.id} className="border rounded p-3 text-sm space-y-2">
              <div className="flex justify-between items-start">
                <div>
                  <p className="font-medium">
                    {MAINTENANCE_TYPE_LABEL[m.type]} — {m.reportedProblem}
                  </p>
                  <p className="text-xs text-gray-500">
                    Técnico: {m.technicianName} · {MAINTENANCE_STATUS_LABEL[m.status]} · Total:{" "}
                    ${m.totalCost.toFixed(2)}
                  </p>
                </div>
                <button
                  className="text-blue-600 text-xs"
                  onClick={() => setOpenId(openId === m.id ? null : m.id)}
                >
                  {openId === m.id ? "Ocultar" : "Detalle"}
                </button>
              </div>

              {openId === m.id && (
                <MaintenanceDetail
                  maintenanceId={m.id}
                  equipmentId={equipmentId}
                  status={m.status}
                  spareParts={m.spareParts}
                  availableSpareParts={spareParts ?? []}
                />
              )}
            </li>
          ))}
          {maintenances?.length === 0 && (
            <p className="text-sm text-gray-500">Sin mantenimientos registrados.</p>
          )}
        </ul>
      )}
    </section>
  );
}

function MaintenanceDetail({
  maintenanceId,
  equipmentId,
  status,
  spareParts,
  availableSpareParts,
}: {
  maintenanceId: string;
  equipmentId: string;
  status: string;
  spareParts: { id: string; sparePartName: string; quantity: number; subtotal: number }[];
  availableSpareParts: { id: string; name: string; active: boolean }[];
}) {
  const queryClient = useQueryClient();
  const [workDone, setWorkDone] = useState("");
  const [selectedPart, setSelectedPart] = useState("");
  const [quantity, setQuantity] = useState(1);

  const closeMutation = useMutation({
    mutationFn: () => closeMaintenance(maintenanceId, { workDone }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenances", equipmentId] });
      queryClient.invalidateQueries({ queryKey: ["equipment", equipmentId] });
    },
  });

  const addPartMutation = useMutation({
    mutationFn: () => addSparePart(maintenanceId, { sparePartId: selectedPart, quantity }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["maintenances", equipmentId] });
      setSelectedPart("");
      setQuantity(1);
    },
  });

  const removePartMutation = useMutation({
    mutationFn: (maintenanceSparePartId: string) =>
      removeSparePart(maintenanceId, maintenanceSparePartId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["maintenances", equipmentId] }),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteMaintenance(maintenanceId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["maintenances", equipmentId] }),
  });

  const isClosed = status === "COMPLETED" || status === "CANCELLED";

  return (
    <div className="border-t pt-2 space-y-3">
      <div>
        <p className="text-xs font-medium mb-1">Repuestos usados</p>
        <ul className="text-xs space-y-1">
          {spareParts.map((sp) => (
            <li key={sp.id} className="flex justify-between">
              <span>
                {sp.sparePartName} x{sp.quantity} — ${sp.subtotal.toFixed(2)}
              </span>
              {!isClosed && (
                <button
                  onClick={() => removePartMutation.mutate(sp.id)}
                  className="text-red-600"
                >
                  Quitar
                </button>
              )}
            </li>
          ))}
          {spareParts.length === 0 && <li className="text-gray-500">Ninguno</li>}
        </ul>

        {!isClosed && (
          <div className="flex gap-2 mt-2">
            <select
              value={selectedPart}
              onChange={(e) => setSelectedPart(e.target.value)}
              className="input flex-1 text-xs"
            >
              <option value="">Repuesto...</option>
              {availableSpareParts
                .filter((sp) => sp.active)
                .map((sp) => (
                  <option key={sp.id} value={sp.id}>
                    {sp.name}
                  </option>
                ))}
            </select>
            <input
              type="number"
              min={1}
              value={quantity}
              onChange={(e) => setQuantity(Number(e.target.value))}
              className="input w-16 text-xs"
            />
            <button
              disabled={!selectedPart || addPartMutation.isPending}
              onClick={() => addPartMutation.mutate()}
              className="bg-gray-700 text-white px-2 py-1 rounded text-xs disabled:opacity-50"
            >
              Agregar
            </button>
          </div>
        )}
      </div>

      {!isClosed && (
        <div className="space-y-1">
          <input
            placeholder="Trabajo realizado (para cerrar)"
            value={workDone}
            onChange={(e) => setWorkDone(e.target.value)}
            className="input w-full text-xs"
          />
          <div className="flex gap-2">
            <button
              disabled={!workDone.trim() || closeMutation.isPending}
              onClick={() => closeMutation.mutate()}
              className="bg-green-600 text-white px-3 py-1 rounded text-xs disabled:opacity-50"
            >
              Cerrar mantenimiento
            </button>
            <button
              onClick={() => deleteMutation.mutate()}
              className="text-red-600 text-xs"
            >
              Eliminar mantenimiento
            </button>
          </div>
        </div>
      )}
    </div>
  );
}