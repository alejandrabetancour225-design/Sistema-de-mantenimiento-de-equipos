import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { changeEquipmentStatus } from "../services/equipmentServices";
import { EQUIPMENT_STATUS_LABEL, type Equipment, type EquipmentStatus } from "../types/equipment";
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
  const [open, setOpen] = useState(false);

  const mutation = useMutation({
    mutationFn: (status: EquipmentStatus) => changeEquipmentStatus(equipment.id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      queryClient.invalidateQueries({ queryKey: ["dashboard"] });
      setOpen(false);
    },
  });

  if (!canChange) return <StatusBadge status={equipment.status} />;

  // "En mantenimiento" lo pone y lo quita el flujo de mantenimiento, no se cambia a mano.
  if (equipment.status === "UNDER_MAINTENANCE") {
    return (
      <span title="Cambia solo al cerrar o cancelar el mantenimiento abierto">
        <StatusBadge status={equipment.status} />
      </span>
    );
  }

  const options = (Object.keys(EQUIPMENT_STATUS_LABEL) as EquipmentStatus[]).filter(
    (st) => st !== "UNDER_MAINTENANCE" && st !== equipment.status
  );

  function choose(status: EquipmentStatus) {
    if (
      status === "DECOMMISSIONED" &&
      !confirm("Dar de baja un equipo es un cambio importante. ¿Continuar?")
    )
      return;
    mutation.mutate(status);
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
          {options.map((st) => (
            <li key={st}>
              <button
                type="button"
                onClick={() => choose(st)}
                className="block w-full px-3 py-1.5 text-left text-sm hover:bg-blue-50"
              >
                {EQUIPMENT_STATUS_LABEL[st]}
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
