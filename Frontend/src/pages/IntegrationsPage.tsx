import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Database, FileSpreadsheet, RefreshCw } from "lucide-react";
import { api } from "../services/api";
import { useAuth } from "../context/authContext";
import { getApiErrorMessage } from "../lib/errors";

type Kind = "file" | "postgres" | "mysql" | "sqlserver" | "mongodb";

interface ColumnProfile {
  column: string;
  type: string;
  null_ratio: number;
  unique_ratio: number;
  distinct: number;
  examples: string[];
}

interface ProfileResponse {
  fields: string[];
  profile: {
    rows_read: number;
    columns: ColumnProfile[];
    suggestion: { fields: Record<string, { column: string; confidence: number }>; key_column: string | null };
    duplicate_keys: string[];
  };
}

interface SourceRow {
  id: string;
  name: string;
  kind: Kind;
  tableName?: string | null;
  fileName?: string | null;
  syncIntervalHours: number;
  lastSyncAt?: string | null;
  lastSyncSummary?: { read: number; created: number; updated: number; skipped: number; errors: string[] } | null;
  active: boolean;
  equipment: number;
}

const FIELD_LABEL: Record<string, string> = {
  internalCode: "Código interno",
  serialNumber: "Número de serie",
  type: "Tipo",
  brand: "Marca",
  model: "Modelo",
  location: "Ubicación",
  acquisitionDate: "Fecha de adquisición",
  acquisitionPrice: "Precio de adquisición",
  operatingSystem: "Sistema operativo",
};

const KIND_LABEL: Record<Kind, string> = {
  file: "Archivo (CSV, Excel o JSON)",
  postgres: "PostgreSQL",
  mysql: "MySQL / MariaDB",
  sqlserver: "SQL Server",
  mongodb: "MongoDB",
};

const EMPTY_CONN = { host: "", port: "", database: "", user: "", password: "", uri: "", table: "" };

function readAsBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result).split(",")[1] ?? "");
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

// Bases de datos de terceros: se leen (solo lectura), se perfilan con Python, el Administrador
// confirma el mapeo y los equipos se importan. Plan Integraciones.
export default function IntegrationsPage() {
  const { plan } = useAuth();
  const queryClient = useQueryClient();
  const [kind, setKind] = useState<Kind>("file");
  const [file, setFile] = useState<{ name: string; base64: string } | null>(null);
  const [conn, setConn] = useState(EMPTY_CONN);
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [mapping, setMapping] = useState<Record<string, string>>({});
  const [keyColumn, setKeyColumn] = useState("");
  const [name, setName] = useState("");
  const [interval, setInterval] = useState(24);

  const sources = useQuery({
    queryKey: ["integration-sources"],
    queryFn: async () => (await api.get<SourceRow[]>("/integrations/Sources")).data,
    enabled: plan === "INTEGRACIONES",
  });

  function connectionPayload() {
    return {
      kind,
      fileName: file?.name,
      fileBase64: file?.base64,
      table: conn.table || undefined,
      host: conn.host || undefined,
      port: conn.port ? Number(conn.port) : undefined,
      database: conn.database || undefined,
      user: conn.user || undefined,
      password: conn.password || undefined,
      uri: conn.uri || undefined,
    };
  }

  const analyze = useMutation({
    mutationFn: async () => (await api.post<ProfileResponse>("/integrations/Profile", connectionPayload(), { timeout: 120_000 })).data,
    onSuccess: (res) => {
      setProfile(res);
      const suggested: Record<string, string> = {};
      Object.entries(res.profile.suggestion.fields).forEach(([field, s]) => (suggested[field] = s.column));
      setMapping(suggested);
      setKeyColumn(res.profile.suggestion.key_column ?? "");
    },
  });

  const save = useMutation({
    mutationFn: async () =>
      (
        await api.post(
          "/integrations/Sources",
          { ...connectionPayload(), name, mapping: Object.fromEntries(Object.entries(mapping).filter(([, c]) => c)), keyColumn, syncIntervalHours: kind === "file" ? 0 : interval },
          { timeout: 180_000 }
        )
      ).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-sources"] });
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
      setProfile(null);
      setFile(null);
      setConn(EMPTY_CONN);
      setName("");
    },
  });

  const sync = useMutation({
    mutationFn: async (id: string) => (await api.post(`/integrations/Sources/${id}/Sync`, null, { timeout: 180_000 })).data,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["integration-sources"] });
      queryClient.invalidateQueries({ queryKey: ["equipment"] });
    },
  });

  if (plan !== "INTEGRACIONES") {
    return (
      <div className="mx-auto max-w-xl space-y-3 p-8">
        <h1 className="text-xl font-semibold">Integraciones</h1>
        <p className="text-sm text-gray-400">
          Conectar las bases de datos de su empresa (Excel, CSV, PostgreSQL, MySQL, SQL Server o MongoDB) e importar
          los equipos automáticamente está disponible en el plan <strong>Integraciones</strong>. Pídale al
          administrador de la plataforma que lo active.
        </p>
      </div>
    );
  }

  const error = analyze.error ?? save.error ?? sync.error ?? sources.error;
  const columns = profile?.profile.columns.map((c) => c.column) ?? [];
  const missingRequired = !mapping.internalCode && !keyColumn;

  return (
    <div className="space-y-5 p-6">
      <div>
        <h1 className="text-xl font-semibold">Integraciones</h1>
        <p className="text-sm text-gray-400">
          Importa los equipos desde la base de datos de tu empresa. La conexión es de <strong>solo lectura</strong>:
          nunca escribimos en tu base. Recomendado: crear un usuario de solo lectura para esta conexión.
        </p>
      </div>

      {error && <p className="text-sm text-red-500">{getApiErrorMessage(error)}</p>}

      <section className="space-y-3 rounded border border-white/10 p-4">
        <h2 className="font-medium">1. Fuente de datos</h2>
        <select value={kind} onChange={(e) => { setKind(e.target.value as Kind); setProfile(null); }} className="input">
          {(Object.keys(KIND_LABEL) as Kind[]).map((k) => (
            <option key={k} value={k}>{KIND_LABEL[k]}</option>
          ))}
        </select>

        {kind === "file" ? (
          <label className="flex items-center gap-2 text-sm">
            <FileSpreadsheet size={18} />
            <input type="file" accept=".csv,.txt,.xlsx,.json"
              onChange={async (e) => {
                const f = e.target.files?.[0];
                setProfile(null);
                setFile(f ? { name: f.name, base64: await readAsBase64(f) } : null);
              }} />
            <span className="text-xs text-gray-400">Máximo 5 MB</span>
          </label>
        ) : (
          <div className="grid gap-2 md:grid-cols-3">
            {kind === "mongodb" ? (
              <input value={conn.uri} onChange={(e) => setConn({ ...conn, uri: e.target.value })}
                placeholder="mongodb://usuario:clave@servidor:27017/base" className="input md:col-span-2" />
            ) : (
              <>
                <input value={conn.host} onChange={(e) => setConn({ ...conn, host: e.target.value })} placeholder="Servidor" className="input" />
                <input value={conn.port} onChange={(e) => setConn({ ...conn, port: e.target.value })} placeholder="Puerto" className="input" />
                <input value={conn.user} onChange={(e) => setConn({ ...conn, user: e.target.value })} placeholder="Usuario (solo lectura)" className="input" />
                <input type="password" value={conn.password} onChange={(e) => setConn({ ...conn, password: e.target.value })} placeholder="Contraseña" className="input" />
              </>
            )}
            <input value={conn.database} onChange={(e) => setConn({ ...conn, database: e.target.value })} placeholder="Base de datos" className="input" />
            <input value={conn.table} onChange={(e) => setConn({ ...conn, table: e.target.value })}
              placeholder={kind === "mongodb" ? "Colección" : "Tabla (o esquema.tabla)"} className="input" />
          </div>
        )}

        <button onClick={() => analyze.mutate()} disabled={analyze.isPending || (kind === "file" && !file)}
          className="flex items-center gap-2 rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50">
          <Database size={16} /> {analyze.isPending ? "Analizando…" : "Analizar datos"}
        </button>
      </section>

      {profile && (
        <section className="space-y-3 rounded border border-white/10 p-4">
          <h2 className="font-medium">2. Lo que encontramos ({profile.profile.rows_read} registros de muestra)</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-white/10 text-left"><th className="py-1">Columna</th><th>Tipo</th><th>Vacíos</th><th>Únicos</th><th>Ejemplos</th></tr>
              </thead>
              <tbody>
                {profile.profile.columns.map((c) => (
                  <tr key={c.column} className="border-b border-white/5">
                    <td className="py-1 font-medium">{c.column}</td>
                    <td>{c.type}</td>
                    <td>{Math.round(c.null_ratio * 100)} %</td>
                    <td>{Math.round(c.unique_ratio * 100)} %</td>
                    <td className="text-gray-400">{c.examples.slice(0, 3).join(" · ")}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <h2 className="font-medium">3. Confirma el mapeo</h2>
          <p className="text-xs text-gray-400">
            Lo propusimos según el nombre y el contenido de cada columna. Las columnas sin campo se guardan como
            atributos adicionales del equipo.
          </p>
          <div className="grid gap-2 md:grid-cols-3">
            {profile.fields.map((field) => {
              const s = profile.profile.suggestion.fields[field];
              return (
                <label key={field} className="text-xs">
                  {FIELD_LABEL[field] ?? field}
                  {s && mapping[field] === s.column && <span className="ml-1 text-green-500">({Math.round(s.confidence * 100)} %)</span>}
                  <select value={mapping[field] ?? ""} onChange={(e) => setMapping({ ...mapping, [field]: e.target.value })} className="input w-full">
                    <option value="">— sin mapear —</option>
                    {columns.map((c) => <option key={c} value={c}>{c}</option>)}
                  </select>
                </label>
              );
            })}
            <label className="text-xs">
              Columna que identifica cada registro
              <select value={keyColumn} onChange={(e) => setKeyColumn(e.target.value)} className="input w-full">
                <option value="">— elige —</option>
                {columns.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
            </label>
          </div>
          {profile.profile.duplicate_keys.length > 0 && (
            <p className="text-xs text-amber-500">
              Hay valores repetidos en la columna llave ({profile.profile.duplicate_keys.slice(0, 5).join(", ")}…): solo se importará el primero.
            </p>
          )}

          <div className="flex flex-wrap items-end gap-2">
            <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nombre de la fuente (ej: ERP de compras)" className="input flex-1" />
            {kind !== "file" && (
              <label className="text-xs">
                Sincronizar cada
                <select value={interval} onChange={(e) => setInterval(Number(e.target.value))} className="input ml-1">
                  <option value={0}>solo manual</option>
                  <option value={1}>hora</option>
                  <option value={6}>6 horas</option>
                  <option value={24}>día</option>
                  <option value={168}>semana</option>
                </select>
              </label>
            )}
            <button onClick={() => save.mutate()} disabled={!name.trim() || !keyColumn || missingRequired || save.isPending}
              className="rounded bg-green-600 px-4 py-2 text-white disabled:opacity-50">
              {save.isPending ? "Importando…" : "Guardar e importar"}
            </button>
          </div>
        </section>
      )}

      <section className="space-y-2">
        <h2 className="font-medium">Fuentes conectadas</h2>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-white/10 text-left"><th className="py-2">Fuente</th><th>Tipo</th><th>Equipos</th><th>Última sincronización</th><th></th></tr>
          </thead>
          <tbody>
            {sources.data?.map((s) => (
              <tr key={s.id} className={`border-b border-white/5 align-top ${s.active ? "" : "opacity-50"}`}>
                <td className="py-2 font-medium">{s.name}<span className="block text-xs text-gray-400">{s.fileName ?? s.tableName}</span></td>
                <td>{KIND_LABEL[s.kind]}{s.syncIntervalHours > 0 ? ` · cada ${s.syncIntervalHours} h` : ""}</td>
                <td>{s.equipment}</td>
                <td className="text-xs">
                  {s.lastSyncAt ? new Date(s.lastSyncAt).toLocaleString() : "—"}
                  {s.lastSyncSummary && (
                    <span className="block text-gray-400">
                      {s.lastSyncSummary.created} nuevos · {s.lastSyncSummary.updated} actualizados · {s.lastSyncSummary.skipped} omitidos
                      {s.lastSyncSummary.errors.length > 0 && <span className="block text-amber-500">{s.lastSyncSummary.errors.slice(0, 3).join(" | ")}</span>}
                    </span>
                  )}
                </td>
                <td className="text-right">
                  {s.active && s.kind !== "file" && (
                    <button onClick={() => sync.mutate(s.id)} disabled={sync.isPending} className="flex items-center gap-1 text-xs text-blue-400">
                      <RefreshCw size={14} /> Sincronizar
                    </button>
                  )}
                </td>
              </tr>
            ))}
            {sources.data?.length === 0 && <tr><td colSpan={5} className="py-4 text-center text-gray-400">Aún no hay fuentes conectadas.</td></tr>}
          </tbody>
        </table>
      </section>
    </div>
  );
}
