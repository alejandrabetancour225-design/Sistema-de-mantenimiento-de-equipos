import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Trash2 } from "lucide-react";
import {
  CLAIM_STATUS_LABEL,
  createContract,
  createLessor,
  getClaims,
  getContracts,
  getLessors,
  returnWholeContract,
  updateClaim,
  type LessorClaim,
  type RentalContractInput,
  type RentalRate,
} from "../services/rentalServices";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

type Tab = "contratos" | "arrendadores" | "reclamos";

const cop = (n: number) =>
  new Intl.NumberFormat("es-CO", { style: "currency", currency: "COP", maximumFractionDigits: 0 }).format(n);

const EMPTY_CONTRACT: RentalContractInput = {
  lessorId: "",
  contractNumber: "",
  startDate: "",
  endDate: "",
  allowsInternalPreventive: false,
  notes: "",
  rates: [{ equipmentType: "", monthlyRate: 0 }],
};

// Equipos alquilados: el cliente toma equipos en alquiler de un arrendador (Q8, Q31).
export default function RentalPage() {
  const { role } = useAuth();
  const canManage = role === "Administrador" || role === "Técnico";
  const isAdmin = role === "Administrador";
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>("contratos");

  const contracts = useQuery({ queryKey: ["rental-contracts"], queryFn: getContracts });
  const lessors = useQuery({ queryKey: ["rental-lessors"], queryFn: getLessors });
  const claims = useQuery({ queryKey: ["rental-claims"], queryFn: getClaims });
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["rental-contracts"] });
    queryClient.invalidateQueries({ queryKey: ["rental-lessors"] });
    queryClient.invalidateQueries({ queryKey: ["rental-claims"] });
    queryClient.invalidateQueries({ queryKey: ["equipment"] });
  };

  // --- Contrato nuevo ---
  const [contract, setContract] = useState<RentalContractInput>(EMPTY_CONTRACT);
  const saveContract = useMutation({
    mutationFn: () =>
      createContract({ ...contract, rates: contract.rates.filter((r) => r.equipmentType.trim()) }),
    onSuccess: () => {
      invalidate();
      setContract(EMPTY_CONTRACT);
    },
  });
  const setRate = (i: number, patch: Partial<RentalRate>) =>
    setContract({ ...contract, rates: contract.rates.map((r, j) => (j === i ? { ...r, ...patch } : r)) });

  const returnAll = useMutation({
    mutationFn: returnWholeContract,
    onSuccess: (res) => {
      invalidate();
      alert(`${res.message}${res.pending.length ? `\nPendientes: ${res.pending.join(", ")}` : ""}`);
    },
  });

  // --- Arrendador nuevo ---
  const [lessor, setLessor] = useState({ name: "", taxId: "", contactName: "", contactEmail: "", contactPhone: "" });
  const saveLessor = useMutation({
    mutationFn: () => createLessor({ ...lessor, active: true }),
    onSuccess: () => {
      invalidate();
      setLessor({ name: "", taxId: "", contactName: "", contactEmail: "", contactPhone: "" });
    },
  });

  // --- Reclamos ---
  const claimMutation = useMutation({
    mutationFn: ({ claim, status }: { claim: LessorClaim; status: LessorClaim["status"] }) => {
      const response =
        status === "OPEN" ? undefined : prompt("Respuesta del arrendador (opcional):", claim.lessorResponse ?? "") ?? undefined;
      return updateClaim(claim.id, { status, lessorResponse: response });
    },
    onSuccess: invalidate,
  });

  const error =
    saveContract.error ?? saveLessor.error ?? returnAll.error ?? claimMutation.error ?? contracts.error;
  const tabClass = (t: Tab) =>
    `px-4 py-2 text-sm rounded-t ${tab === t ? "bg-white/10 font-medium" : "text-gray-400 hover:text-white"}`;

  return (
    <div className="space-y-4 p-6">
      <div>
        <h1 className="text-xl font-semibold">Equipos alquilados</h1>
        <p className="text-sm text-gray-400">
          Contratos con tarifa mensual por tipo de equipo, devoluciones y reclamos al arrendador.
        </p>
      </div>

      <div className="flex gap-1 border-b border-white/10">
        <button className={tabClass("contratos")} onClick={() => setTab("contratos")}>Contratos</button>
        <button className={tabClass("arrendadores")} onClick={() => setTab("arrendadores")}>Arrendadores</button>
        <button className={tabClass("reclamos")} onClick={() => setTab("reclamos")}>
          Reclamos {claims.data?.filter((c) => c.status !== "RESOLVED").length ? `(${claims.data.filter((c) => c.status !== "RESOLVED").length})` : ""}
        </button>
      </div>

      {error && <p className="text-sm text-red-500">{getApiErrorMessage(error)}</p>}

      {tab === "contratos" && (
        <div className="space-y-4">
          {canManage && (
            <form
              onSubmit={(e) => {
                e.preventDefault();
                saveContract.mutate();
              }}
              className="space-y-2 rounded border border-white/10 p-3"
            >
              <p className="text-sm font-medium">Nuevo contrato</p>
              <div className="grid gap-2 md:grid-cols-4">
                <select value={contract.lessorId} onChange={(e) => setContract({ ...contract, lessorId: e.target.value })}
                  className="input" required>
                  <option value="">Arrendador</option>
                  {lessors.data?.filter((l) => l.active).map((l) => (
                    <option key={l.id} value={l.id}>{l.name}</option>
                  ))}
                </select>
                <input value={contract.contractNumber} onChange={(e) => setContract({ ...contract, contractNumber: e.target.value })}
                  placeholder="Número de contrato" className="input" required />
                <label className="text-xs text-gray-400">Inicio
                  <input type="date" value={contract.startDate} onChange={(e) => setContract({ ...contract, startDate: e.target.value })}
                    className="input w-full" required />
                </label>
                <label className="text-xs text-gray-400">Fin
                  <input type="date" value={contract.endDate} min={contract.startDate}
                    onChange={(e) => setContract({ ...contract, endDate: e.target.value })} className="input w-full" required />
                </label>
              </div>
              <p className="text-xs text-gray-400">Tarifa mensual (COP) por tipo de equipo:</p>
              {contract.rates.map((r, i) => (
                <div key={i} className="flex gap-2">
                  <input value={r.equipmentType} onChange={(e) => setRate(i, { equipmentType: e.target.value })}
                    placeholder="Tipo (ej: Portátil)" className="input flex-1" />
                  <input type="number" min={0} step={1000} value={r.monthlyRate}
                    onChange={(e) => setRate(i, { monthlyRate: Number(e.target.value) })} className="input w-40" />
                  <button type="button" className="text-gray-400 hover:text-red-400" aria-label="Quitar tarifa"
                    onClick={() => setContract({ ...contract, rates: contract.rates.filter((_, j) => j !== i) })}>
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
              <button type="button" className="flex items-center gap-1 text-xs text-blue-400"
                onClick={() => setContract({ ...contract, rates: [...contract.rates, { equipmentType: "", monthlyRate: 0 }] })}>
                <Plus size={14} /> Agregar tipo
              </button>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={contract.allowsInternalPreventive}
                  onChange={(e) => setContract({ ...contract, allowsInternalPreventive: e.target.checked })} />
                El contrato permite que nuestro técnico haga mantenimiento preventivo
              </label>
              <button type="submit" disabled={saveContract.isPending}
                className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-50">Crear contrato</button>
            </form>
          )}

          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-white/10 text-left">
                <th className="py-2">Contrato</th><th>Arrendador</th><th>Vigencia</th><th>Tarifas</th>
                <th>Equipos</th><th>Costo mensual</th><th></th>
              </tr>
            </thead>
            <tbody>
              {contracts.data?.map((c) => (
                <tr key={c.id} className="border-b border-white/5 align-top">
                  <td className="py-2 font-medium">
                    {c.contractNumber}
                    <span className="block text-xs text-gray-400">{c.status === "ACTIVE" ? "Activo" : "Terminado"}</span>
                  </td>
                  <td>{c.lessorName}</td>
                  <td>
                    {c.startDate} → {c.endDate}
                    {c.status === "ACTIVE" && c.daysToExpiry <= 30 && (
                      <span className={`block text-xs ${c.daysToExpiry < 0 ? "text-red-500" : "text-amber-500"}`}>
                        {c.daysToExpiry < 0 ? `Vencido hace ${-c.daysToExpiry} días` : `Vence en ${c.daysToExpiry} días`}
                      </span>
                    )}
                  </td>
                  <td className="text-xs">
                    {c.rates.map((r) => (
                      <span key={r.equipmentType} className="block">{r.equipmentType}: {cop(r.monthlyRate)}</span>
                    ))}
                  </td>
                  <td>{c.activeEquipment}{c.returnedEquipment ? ` (+${c.returnedEquipment} devueltos)` : ""}</td>
                  <td>{cop(c.monthlyCost)}</td>
                  <td className="text-right text-xs">
                    {isAdmin && c.status === "ACTIVE" && c.activeEquipment > 0 && (
                      <button className="text-red-500" disabled={returnAll.isPending}
                        onClick={() => {
                          if (confirm(`¿Devolver todos los equipos del contrato ${c.contractNumber}?`)) returnAll.mutate(c.id);
                        }}>
                        Devolver todo
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {contracts.data?.length === 0 && (
                <tr><td colSpan={7} className="py-4 text-center text-gray-400">No hay contratos de alquiler.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {tab === "arrendadores" && (
        <div className="space-y-4">
          {canManage && (
            <form
              onSubmit={(e) => {
                e.preventDefault();
                saveLessor.mutate();
              }}
              className="grid gap-2 rounded border border-white/10 p-3 md:grid-cols-6"
            >
              <input value={lessor.name} onChange={(e) => setLessor({ ...lessor, name: e.target.value })}
                placeholder="Empresa arrendadora" className="input md:col-span-2" required />
              <input value={lessor.taxId} onChange={(e) => setLessor({ ...lessor, taxId: e.target.value })}
                placeholder="NIT" className="input" />
              <input value={lessor.contactName} onChange={(e) => setLessor({ ...lessor, contactName: e.target.value })}
                placeholder="Contacto" className="input" required />
              <input type="email" value={lessor.contactEmail} onChange={(e) => setLessor({ ...lessor, contactEmail: e.target.value })}
                placeholder="Correo del contacto" className="input" required />
              <button type="submit" disabled={saveLessor.isPending}
                className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-50">Agregar</button>
            </form>
          )}
          <table className="w-full text-sm">
            <thead><tr className="border-b border-white/10 text-left"><th className="py-2">Arrendador</th><th>NIT</th><th>Contacto</th><th>Correo</th></tr></thead>
            <tbody>
              {lessors.data?.map((l) => (
                <tr key={l.id} className="border-b border-white/5">
                  <td className="py-2 font-medium">{l.name}</td><td>{l.taxId}</td><td>{l.contactName}</td><td>{l.contactEmail}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === "reclamos" && (
        <div className="space-y-2">
          <p className="text-xs text-gray-400">
            Los reclamos se crean desde el detalle de un equipo alquilado y se envían por correo al arrendador.
          </p>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-white/10 text-left">
                <th className="py-2">Equipo</th><th>Arrendador</th><th>Falla</th><th>Estado</th><th>Respuesta</th><th></th>
              </tr>
            </thead>
            <tbody>
              {claims.data?.map((c) => (
                <tr key={c.id} className="border-b border-white/5 align-top">
                  <td className="py-2 font-medium">{c.equipmentCode}<span className="block text-xs text-gray-400">{c.contractNumber}</span></td>
                  <td>{c.lessorName}</td>
                  <td className="max-w-xs">{c.description}
                    <span className="block text-xs text-gray-400">
                      {new Date(c.createdAt).toLocaleDateString()} · {c.notifiedAt ? "correo enviado" : "correo no enviado"}
                    </span>
                  </td>
                  <td>{CLAIM_STATUS_LABEL[c.status]}</td>
                  <td className="max-w-xs text-xs">{c.lessorResponse}</td>
                  <td className="space-x-2 text-right text-xs">
                    {canManage && c.status === "OPEN" && (
                      <button className="text-blue-500" onClick={() => claimMutation.mutate({ claim: c, status: "ANSWERED" })}>Respondido</button>
                    )}
                    {canManage && c.status !== "RESOLVED" && (
                      <button className="text-green-500" onClick={() => claimMutation.mutate({ claim: c, status: "RESOLVED" })}>Resuelto</button>
                    )}
                  </td>
                </tr>
              ))}
              {claims.data?.length === 0 && (
                <tr><td colSpan={6} className="py-4 text-center text-gray-400">No hay reclamos.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
