using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IReportPdfGenerator
{
    byte[] GenerateEquipmentsPdf(string title, List<EquipmentReportRow> rows, ReportFilter filter);
    byte[] GenerateMaintenancesPdf(string title, List<MaintenanceReportRow> rows, ReportFilter filter);
    byte[] GenerateCostsPdf(string title, List<CostReportRow> rows, ReportFilter filter);
}
