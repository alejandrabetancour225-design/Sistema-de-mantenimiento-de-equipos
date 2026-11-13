import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../services/api";
import { getApiErrorMessage } from "../lib/errors";
import { PASSWORD_RULE_MESSAGE, strongPassword } from "../schemas/authSchemas";

type Plan = "BASICO" | "INTEGRACIONES";

interface OrganizationRow {
  id: string;
  name: string;
  plan: Plan;
  active: boolean;
  createdAt: string;
  users: number;
  equipment: number;
}

const PLAN_LABEL: Record<Plan, string> = { BASICO: "Básico", INTEGRACIONES: "Integraciones" };

const EMPTY = { name: "", plan: "BASICO" as Plan, adminName: "", adminEmail: "", adminPassword: "" };

// Panel del SuperAdmin: organizaciones (empresas) y su plan de membresía.
// El plan se asigna a mano; no hay pagos ni cobros (Q5).
export default function OrganizationsPage() {
  const queryClient = useQueryClient();
  const { data, isLoading } = useQuery({
    queryKey: ["organizations"],
    queryFn: async () => (await api.get<OrganizationRow[]>("/organizations")).data,
  });
  const [form, setForm] = useState(EMPTY);
  const [formError, setFormError] = useState("");
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["organizations"] });

  const create = useMutation({
    mutationFn: () =>
      api.post("/organizations", {
        name: form.name,
        plan: form.plan,
        administrator: {
          fullName: form.adminName,
          email: form.adminEmail,
          role: "Administrador",
          temporaryPassword: form.adminPassword,
        },
      }),
    onSuccess: () => {
      invalidate();
      setForm(EMPTY);
    },
  });

  const update = useMutation({
    mutationFn: (org: OrganizationRow) =>
      api.put(`/organizations/${org.id}`, { name: org.name, plan: org.plan, active: org.active }),
    onSuccess: invalidate,
  });

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setFormError("");
    if (!strongPassword.safeParse(form.adminPassword).success) {
      setFormError(PASSWORD_RULE_MESSAGE);
      return;
    }
    create.mutate();
  }

  return (
    <div className="space-y-4 p-6">
      <div>
        <h1 className="text-xl font-semibold">Organizaciones</h1>
        <p className="text-sm text-gray-400">
          Cada organización ve solo sus propios datos. El plan Integraciones habilita conexión a bases de datos
          de terceros, minería de datos, reportes generados por IA e información de mercado.
        </p>
      </div>

      <form onSubmit={submit} className="grid gap-2 rounded border border-white/10 p-3 md:grid-cols-3">
        <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })}
          placeholder="Nombre de la organización" className="input" required />
        <select value={form.plan} onChange={(e) => setForm({ ...form, plan: e.target.value as Plan })} className="input">
          <option value="BASICO">Plan Básico</option>
          <option value="INTEGRACIONES">Plan Integraciones</option>
        </select>
        <span />
        <input value={form.adminName} onChange={(e) => setForm({ ...form, adminName: e.target.value })}
          placeholder="Nombre del administrador" className="input" required />
        <input type="email" value={form.adminEmail} onChange={(e) => setForm({ ...form, adminEmail: e.target.value })}
          placeholder="Correo del administrador" className="input" required />
        <input value={form.adminPassword} onChange={(e) => setForm({ ...form, adminPassword: e.target.value })}
          placeholder="Contraseña temporal" className="input" required />
        <button type="submit" disabled={create.isPending}
          className="rounded bg-blue-600 px-3 py-2 text-white disabled:opacity-50 md:col-span-3">
          Crear organización y su administrador
        </button>
        {(formError || create.error) && (
          <p className="text-sm text-red-500 md:col-span-3">{formError || getApiErrorMessage(create.error)}</p>
        )}
      </form>

      {update.error && <p className="text-sm text-red-500">{getApiErrorMessage(update.error)}</p>}

      {isLoading ? (
        <p className="text-gray-400">Cargando…</p>
      ) : (
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-white/10 text-left">
              <th className="py-2">Organización</th>
              <th>Plan</th>
              <th>Usuarios</th>
              <th>Equipos</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            {data?.map((org) => (
              <tr key={org.id} className="border-b border-white/5">
                <td className="py-2 font-medium">{org.name}</td>
                <td>
                  <select
                    value={org.plan}
                    onChange={(e) => update.mutate({ ...org, plan: e.target.value as Plan })}
                    className="rounded border px-2 py-1"
                  >
                    {(Object.keys(PLAN_LABEL) as Plan[]).map((p) => (
                      <option key={p} value={p}>
                        {PLAN_LABEL[p]}
                      </option>
                    ))}
                  </select>
                </td>
                <td>{org.users}</td>
                <td>{org.equipment}</td>
                <td>
                  <button
                    onClick={() => {
                      if (!org.active || confirm(`¿Desactivar ${org.name}? Sus usuarios no podrán entrar.`))
                        update.mutate({ ...org, active: !org.active });
                    }}
                    className={`rounded px-3 py-1 text-xs font-medium ${
                      org.active ? "bg-green-100 text-green-800" : "bg-gray-200 text-gray-600"
                    }`}
                  >
                    {org.active ? "Activa" : "Inactiva"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
