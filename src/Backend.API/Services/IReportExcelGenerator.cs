using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IReportExcelGenerator
{
    byte[] GenerateEquipmentsExcel(string title, List<EquipmentReportRow> rows);
    byte[] GenerateMaintenancesExcel(string title, List<MaintenanceReportRow> rows);
    byte[] GenerateCostsExcel(string title, List<CostReportRow> rows);
}
