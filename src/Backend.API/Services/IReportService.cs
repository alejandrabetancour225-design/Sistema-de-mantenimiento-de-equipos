using Backend.API.DTOs;

namespace Backend.API.Services;

public interface IReportService
{
    Task<byte[]> GenerateEquipmentsReportAsync(ReportFilter filter, ReportFormat format);
    Task<byte[]> GenerateMaintenancesReportAsync(ReportFilter filter, ReportFormat format);
    Task<byte[]> GenerateCostsReportAsync(ReportFilter filter, ReportFormat format);
}

public enum ReportFormat
{
    Pdf,
    Excel
}
