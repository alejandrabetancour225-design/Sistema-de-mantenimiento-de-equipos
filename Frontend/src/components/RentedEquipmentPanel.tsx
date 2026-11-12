import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { createClaim, registerReplacement, returnEquipment } from "../services/rentalServices";
import type { Equipment } from "../types/equipment";
import { getApiErrorMessage } from "../lib/errors";
import type { Role } from "../types/auth";

// Datos y acciones de un equipo alquilado: reclamo al arrendador, devolución y reemplazo.
export default function RentedEquipmentPanel({ equipment, role }: { equipment: Equipment; role: Role | null }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const canManage = role === "Administrador" || role === "Técnico";
  const isAdmin = role === "Administrador";
  const [claimText, setClaimText] = useState("");
  const [message, setMessage] = useState("");
  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ["equipment"] });
    queryClient.invalidateQueries({ queryKey: ["rental-claims"] });
    queryClient.invalidateQueries({ queryKey: ["rental-contracts"] });
  };

  const claim = useMutation({
    mutationFn: () => createClaim({ equipmentId: equipment.id, description: claimText.trim() }),
    onSuccess: (res) => {
      refresh();
      setClaimText("");
      setMessage(res.notifiedAt
        ? `Reclamo registrado y enviado a ${res.lessorEmail}.`
        : "Reclamo registrado. El correo no se pudo enviar (revisa la configuración SMTP).");
    },
  });
  const giveBack = useMutation({ mutationFn: () => returnEquipment(equipment.id), onSuccess: refresh });
  const replace = useMutation({
    mutationFn: (d: { internalCode: string; serialNumber: string; brand: string; model: string }) =>
      registerReplacement(equipment.id, d),
    onSuccess: (res) => {
      refresh();
      navigate(`/equipos/${res.id}`);
    },
  });

  function askReplacement() {
    const internalCode = prompt("Código interno del equipo de reemplazo:");
    if (!internalCode) return;
    const serialNumber = prompt("Número de serie del reemplazo:");
    if (!serialNumber) return;
    const brand = prompt("Marca:", equipment.brand) ?? equipment.brand;
    const model = prompt("Modelo:", equipment.model) ?? equipment.model;
    replace.mutate({ internalCode, serialNumber, brand, model });
  }

  const error = claim.error ?? giveBack.error ?? replace.error;
  const returnable = equipment.status === "AVAILABLE" || equipment.status === "OUT_OF_SERVICE";

  return (
    <section className="space-y-2 rounded border border-blue-500/30 p-3 text-sm">
      <h2 className="font-medium">Equipo alquilado</h2>
      <p className="text-xs text-gray-400">
        Contrato {equipment.contractNumber} · {equipment.lessorName}.{" "}
        {equipment.allowsInternalPreventive
          ? "El contrato permite mantenimiento preventivo interno."
          : "El mantenimiento lo hace el arrendador."}{" "}
        Las fallas se reportan al arrendador con un reclamo.
      </p>

      {canManage && equipment.status !== "RETURNED" && (
        <div className="flex gap-2">
          <input value={claimText} onChange={(e) => setClaimText(e.target.value)}
            placeholder="Describe la falla para el arrendador" className="input flex-1" />
          <button disabled={claimText.trim().length < 10 || claim.isPending} onClick={() => claim.mutate()}
            className="rounded bg-amber-600 px-3 py-1 text-white disabled:opacity-50">
            Enviar reclamo
          </button>
        </div>
      )}

      <div className="flex flex-wrap gap-3 text-xs">
        {isAdmin && equipment.status !== "RETURNED" && (
          <button
            disabled={!returnable || giveBack.isPending}
            title={returnable ? undefined : "Libera su asignación o cierra su mantenimiento antes de devolverlo"}
            onClick={() => {
              if (confirm("¿Devolver este equipo al arrendador? Queda en el historial y no vuelve a servicio."))
                giveBack.mutate();
            }}
            className="text-red-500 disabled:opacity-50"
          >
            Devolver al arrendador
          </button>
        )}
        {canManage && equipment.status === "RETURNED" && (
          <button onClick={askReplacement} className="text-blue-500">
            Registrar equipo de reemplazo
          </button>
        )}
        {equipment.replacesEquipmentId && (
          <button onClick={() => navigate(`/equipos/${equipment.replacesEquipmentId}`)} className="text-gray-400 underline">
            Reemplaza a otro equipo devuelto
          </button>
        )}
      </div>

      {message && <p className="text-xs text-green-500">{message}</p>}
      {error && <p className="text-xs text-red-500">{getApiErrorMessage(error)}</p>}
    </section>
  );
}
