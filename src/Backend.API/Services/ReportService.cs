using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly IReportPdfGenerator _pdfGenerator;
    private readonly IReportExcelGenerator _excelGenerator;

    public ReportService(
        AppDbContext context,
        IReportPdfGenerator pdfGenerator,
        IReportExcelGenerator excelGenerator)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _excelGenerator = excelGenerator;
    }

    public async Task<byte[]> GenerateEquipmentsReportAsync(ReportFilter filter, ReportFormat format)
    {
        var query = _context.Equipments
            .AsNoTracking()
            .Include(e => e.Assignments!)
                .ThenInclude(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(e => e.Type == filter.Type);

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<EquipmentStatus>(filter.Status, true, out var status))
            query = query.Where(e => e.Status == status);

        if (filter.AssignedTo.HasValue)
            query = query.Where(e => e.Assignments!
                .Any(a => a.UserId == filter.AssignedTo.Value
                    && a.Status == AssignmentStatus.ACTIVE));

        var equipments = await query.OrderBy(e => e.InternalCode).ToListAsync();

        var rows = equipments.Select(e => new EquipmentReportRow
        {
            InternalCode = e.InternalCode,
            SerialNumber = e.SerialNumber,
            Type = e.Type,
            Brand = e.Brand,
            Model = e.Model,
            Location = e.Location,
            Status = e.Status.ToString(),
            AcquisitionPrice = e.AcquisitionPrice,
            AssignedTo = e.Assignments?
                .FirstOrDefault(a => a.Status == AssignmentStatus.ACTIVE)?.User?.FullName
        }).ToList();

        var title = "Reporte de Equipos";
        return format == ReportFormat.Pdf
            ? _pdfGenerator.GenerateEquipmentsPdf(title, rows, filter)
            : _excelGenerator.GenerateEquipmentsExcel(title, rows);
    }

    public async Task<byte[]> GenerateMaintenancesReportAsync(ReportFilter filter, ReportFormat format)
    {
        var query = _context.Maintenances
            .AsNoTracking()
            .Include(m => m.Equipment)
            .Include(m => m.Technician)
            .Include(m => m.SpareParts)
            .AsQueryable();

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.StartedAt >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(m => m.StartedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Type)
            && Enum.TryParse<MaintenanceType>(filter.Type, true, out var type))
            query = query.Where(m => m.Type == type);

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<MaintenanceStatus>(filter.Status, true, out var status))
            query = query.Where(m => m.Status == status);

        if (filter.TechnicianId.HasValue)
            query = query.Where(m => m.TechnicianId == filter.TechnicianId.Value);

        var maintenances = await query.OrderByDescending(m => m.StartedAt).ToListAsync();

        var rows = maintenances.Select(m =>
        {
            var spareCost = m.SpareParts?.Sum(sp => sp.Quantity * sp.UnitCostAtUse) ?? 0;
            return new MaintenanceReportRow
            {
                EquipmentInternalCode = m.Equipment?.InternalCode ?? string.Empty,
                Type = m.Type.ToString(),
                Status = m.Status.ToString(),
                TechnicianName = m.Technician?.FullName ?? string.Empty,
                StartedAt = m.StartedAt,
                CompletedAt = m.CompletedAt,
                LaborCost = m.LaborCost,
                OtherCosts = m.OtherCosts,
                SparePartsCost = spareCost,
                TotalCost = m.LaborCost + m.OtherCosts + spareCost,
                ReportedProblem = m.ReportedProblem
            };
        }).ToList();

        var title = "Reporte de Mantenimientos";
        return format == ReportFormat.Pdf
            ? _pdfGenerator.GenerateMaintenancesPdf(title, rows, filter)
            : _excelGenerator.GenerateMaintenancesExcel(title, rows);
    }

    public async Task<byte[]> GenerateCostsReportAsync(ReportFilter filter, ReportFormat format)
    {
        var query = _context.Maintenances
            .AsNoTracking()
            .Include(m => m.Equipment)
            .Include(m => m.SpareParts)
            .Where(m => m.Status == MaintenanceStatus.COMPLETED)
            .AsQueryable();

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(m => m.CompletedAt >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(m => m.CompletedAt <= to);
        }

        if (filter.EquipmentId.HasValue)
            query = query.Where(m => m.EquipmentId == filter.EquipmentId.Value);

        var maintenances = await query.ToListAsync();

        var rows = maintenances
            .GroupBy(m => m.Equipment?.InternalCode ?? "Desconocido")
            .Select(g => new CostReportRow
            {
                EquipmentInternalCode = g.Key,
                MaintenanceCount = g.Count(),
                TotalLaborCost = g.Sum(m => m.LaborCost),
                TotalSparePartsCost = g.Sum(m => m.SpareParts?.Sum(sp => sp.Quantity * sp.UnitCostAtUse) ?? 0),
                TotalOtherCosts = g.Sum(m => m.OtherCosts),
                TotalCost = g.Sum(m => m.LaborCost + m.OtherCosts
                    + (m.SpareParts?.Sum(sp => sp.Quantity * sp.UnitCostAtUse) ?? 0))
            })
            .OrderByDescending(r => r.TotalCost)
            .ToList();

        var title = "Reporte de Costos";
        return format == ReportFormat.Pdf
            ? _pdfGenerator.GenerateCostsPdf(title, rows, filter)
            : _excelGenerator.GenerateCostsExcel(title, rows);
    }
}
