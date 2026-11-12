import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { changeEquipmentStatus, deleteEquipment } from "../services/equipmentServices";
import type { Equipment } from "../types/equipment";
import { useAuth } from "../context/authContext";
import { manualStatusActions, statusLockedReason, type StatusAction } from "../lib/equipmentTransitions";
import { getApiErrorMessage } from "../lib/errors";
import StatusBadge from "./StatusBadge";

// Muestra el estado del equipo; si el usuario puede gestionarlo, al hacer clic
// se abre la lista de estados a los que puede pasar.
export default function EquipmentStatusMenu({
  equipment,
  canChange,
}: {
  equipment: Equipment;
  canChange: boolean;
}) {
  const queryClient = useQueryClient();
  const { role } = useAuth();
  const [open, setOpen] = useState(false);

  const mutation = useMutation({
    mutationFn: async (action: StatusAction): Promise<void> => {
      if (action.kind === "decommission") await deleteEquipment(equipment.id);
      else await changeEquipmentStatus(equipment.id, action.target);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setOpen(false);
    },
  });

  if (!canChange) return <StatusBadge status={equipment.status} />;

  // R4: solo se ofrecen los cambios manuales que la regla maestra permite.
  const options = manualStatusActions(equipment.status, role);
  if (options.length === 0) {
    return (
      <span title={statusLockedReason(equipment.status) ?? undefined}>
        <StatusBadge status={equipment.status} />
      </span>
    );
  }

  function choose(action: StatusAction) {
    if (
      action.kind === "decommission" &&
      !confirm("Dar de baja un equipo es definitivo: no podrá volver a servicio. ¿Continuar?")
    )
      return;
    mutation.mutate(action);
  }

  return (
    <div
      className="relative inline-block"
      onBlur={(e) => {
        if (!e.currentTarget.contains(e.relatedTarget as Node | null)) setOpen(false);
      }}
    >
      <button
        type="button"
        disabled={mutation.isPending}
        onClick={() => {
          mutation.reset();
          setOpen((v) => !v);
        }}
        title="Haz clic para cambiar el estado"
        className="inline-flex items-center gap-1 rounded-full ring-blue-300 hover:ring-2 disabled:opacity-60"
      >
        <StatusBadge status={equipment.status} />
        <span className="pr-1 text-xs text-gray-500">{mutation.isPending ? "…" : "▾"}</span>
      </button>

      {open && (
        <ul
          // Evita que el menú se cierre por pérdida de foco antes de registrar el clic
          onMouseDown={(e) => e.preventDefault()}
          className="absolute left-0 z-20 mt-1 min-w-[11rem] rounded border bg-white py-1 shadow-lg"
        >
          <li className="px-3 py-1 text-xs text-gray-500">Cambiar a:</li>
          {options.map((action) => (
            <li key={action.target}>
              <button
                type="button"
                onClick={() => choose(action)}
                className="block w-full px-3 py-1.5 text-left text-sm hover:bg-blue-50"
              >
                {action.label}
              </button>
            </li>
          ))}
        </ul>
      )}

      {mutation.error && (
        <p className="mt-1 max-w-[16rem] text-xs text-red-600">
          {getApiErrorMessage(mutation.error)}
        </p>
      )}
    </div>
  );
}
