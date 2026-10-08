import { useState } from "react";
import { FileDown, FileSpreadsheet } from "lucide-react";
import {
  downloadEquipmentsReport,
  downloadMaintenancesReport,
  downloadCostsReport,
} from "../services/reportServices";
import type { ReportFormat } from "../types/report";

type ReportFilters = {
  from?: string;
  to?: string;
  type?: string;
};

type ReportFn = (filters: ReportFilters, format: ReportFormat) => Promise<void>;

interface ReportCardProps {
  title: string;
  description: string;
  reportKey: string;
  reportFn: ReportFn;
  loading: string | null;
  onDownload: (fn: ReportFn, key: string, format: ReportFormat) => void;
}

function ReportCard({
  title,
  description,
  reportKey,
  reportFn,
  loading,
  onDownload,
}: ReportCardProps) {
  return (
    <div className="rounded-xl border border-white/10 bg-[#0f1729] p-6">
      <h2 className="text-lg font-semibold text-white">{title}</h2>
      <p className="mt-1 text-sm text-gray-400">{description}</p>
      <div className="mt-4 flex gap-2">
        <button
          onClick={() => onDownload(reportFn, reportKey, "pdf")}
          disabled={loading === `${reportKey}-pdf`}
          className="flex items-center gap-2 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
        >
          <FileDown size={16} />
          {loading === `${reportKey}-pdf` ? "Generando..." : "Descargar PDF"}
        </button>
        <button
          onClick={() => onDownload(reportFn, reportKey, "excel")}
          disabled={loading === `${reportKey}-excel`}
          className="flex items-center gap-2 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
        >
          <FileSpreadsheet size={16} />
          {loading === `${reportKey}-excel` ? "Generando..." : "Descargar Excel"}
        </button>
      </div>
    </div>
  );
}

export default function ReportsPage() {
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [type, setType] = useState("");
  const [loading, setLoading] = useState<string | null>(null);

  async function handleDownload(
    reportFn: ReportFn,
    reportKey: string,
    format: ReportFormat
  ) {
    setLoading(`${reportKey}-${format}`);
    try {
      await reportFn(
        {
          from: from || undefined,
          to: to || undefined,
          type: type || undefined,
        },
        format
      );
    } catch {
      alert("Error al generar el reporte");
    } finally {
      setLoading(null);
    }
  }

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold text-white">Reportes</h1>
        <p className="text-sm text-gray-400">
          Genera reportes gerenciales filtrables y exportables
        </p>
      </div>

      <div className="rounded-xl border border-white/10 bg-[#0f1729] p-6">
        <h2 className="mb-4 text-lg font-semibold text-white">Filtros</h2>
        <div className="flex flex-wrap gap-3">
          <div>
            <label className="block text-xs text-gray-400 mb-1">Desde</label>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="rounded border border-white/10 bg-[#111527] px-3 py-2 text-sm text-white"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Hasta</label>
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="rounded border border-white/10 bg-[#111527] px-3 py-2 text-sm text-white"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-400 mb-1">Tipo</label>
            <input
              type="text"
              value={type}
              onChange={(e) => setType(e.target.value)}
              placeholder="Laptop, Monitor..."
              className="rounded border border-white/10 bg-[#111527] px-3 py-2 text-sm text-white"
            />
          </div>
        </div>
      </div>

      <ReportCard
        title="Reporte de Equipos"
        description="Inventario completo de equipos con su estado y asignación actual"
        reportKey="equipos"
        reportFn={downloadEquipmentsReport}
        loading={loading}
        onDownload={handleDownload}
      />

      <ReportCard
        title="Reporte de Mantenimientos"
        description="Historial de mantenimientos preventivos y correctivos con costos"
        reportKey="mantenimientos"
        reportFn={downloadMaintenancesReport}
        loading={loading}
        onDownload={handleDownload}
      />

      <ReportCard
        title="Reporte de Costos"
        description="Costos acumulados por equipo: mano de obra, repuestos y otros"
        reportKey="costos"
        reportFn={downloadCostsReport}
        loading={loading}
        onDownload={handleDownload}
      />
    </div>
  );
}
