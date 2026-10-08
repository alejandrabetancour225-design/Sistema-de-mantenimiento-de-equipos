import { api } from "./api";
import type { ReportFilters, ReportFormat } from "../types/report";

function buildQuery(filters: ReportFilters, format: ReportFormat): string {
  const params = new URLSearchParams();
  params.set("format", format);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.type) params.set("type", filters.type);
  if (filters.status) params.set("status", filters.status);
  if (filters.assignedTo) params.set("assignedTo", filters.assignedTo);
  if (filters.equipmentId) params.set("equipmentId", filters.equipmentId);
  if (filters.technicianId) params.set("technicianId", filters.technicianId);
  return params.toString();
}

async function downloadReport(path: string, filters: ReportFilters, format: ReportFormat) {
  const query = buildQuery(filters, format);
  const res = await api.get(`${path}?${query}`, {
    responseType: "blob",
  });

  const contentType =
    typeof res.headers["content-type"] === "string"
      ? res.headers["content-type"]
      : "application/octet-stream";
  const extension = format === "pdf" ? "pdf" : "xlsx";
  const fileName = `reporte.${extension}`;

  const blob = new Blob([res.data], { type: contentType });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}

export async function downloadEquipmentsReport(filters: ReportFilters, format: ReportFormat) {
  return downloadReport("/reports/GetEquipments", filters, format);
}

export async function downloadMaintenancesReport(filters: ReportFilters, format: ReportFormat) {
  return downloadReport("/reports/GetMaintenances", filters, format);
}

export async function downloadCostsReport(filters: ReportFilters, format: ReportFormat) {
  return downloadReport("/reports/GetCosts", filters, format);
}
