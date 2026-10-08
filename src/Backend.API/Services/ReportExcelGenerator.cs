using Backend.API.DTOs;
using ClosedXML.Excel;

namespace Backend.API.Services;

public class ReportExcelGenerator : IReportExcelGenerator
{
    public byte[] GenerateEquipmentsExcel(string title, List<EquipmentReportRow> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Equipos");

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14);

        var headers = new[] { "Código", "Serie", "Tipo", "Marca", "Modelo", "Ubicación", "Estado", "Precio", "Asignado a" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(2, i + 1).Value = headers[i];

        StyleHeader(ws.Range(2, 1, 2, headers.Length));

        int row = 3;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.InternalCode;
            ws.Cell(row, 2).Value = r.SerialNumber;
            ws.Cell(row, 3).Value = r.Type;
            ws.Cell(row, 4).Value = r.Brand;
            ws.Cell(row, 5).Value = r.Model;
            ws.Cell(row, 6).Value = r.Location;
            ws.Cell(row, 7).Value = r.Status;
            if (r.AcquisitionPrice.HasValue)
                ws.Cell(row, 8).Value = r.AcquisitionPrice.Value;
            ws.Cell(row, 9).Value = r.AssignedTo ?? "—";
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenerateMaintenancesExcel(string title, List<MaintenanceReportRow> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mantenimientos");

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(14);

        var headers = new[] { "Equipo", "Tipo", "Estado", "Técnico", "Inicio", "Fin", "M. Obra", "Otros", "Repuestos", "Total" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(2, i + 1).Value = headers[i];

        StyleHeader(ws.Range(2, 1, 2, headers.Length));

        int row = 3;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.EquipmentInternalCode;
            ws.Cell(row, 2).Value = r.Type;
            ws.Cell(row, 3).Value = r.Status;
            ws.Cell(row, 4).Value = r.TechnicianName;
            ws.Cell(row, 5).Value = r.StartedAt;
            if (r.CompletedAt.HasValue)
                ws.Cell(row, 6).Value = r.CompletedAt.Value;
            ws.Cell(row, 7).Value = r.LaborCost;
            ws.Cell(row, 8).Value = r.OtherCosts;
            ws.Cell(row, 9).Value = r.SparePartsCost;
            ws.Cell(row, 10).Value = r.TotalCost;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenerateCostsExcel(string title, List<CostReportRow> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Costos");

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 6).Merge().Style.Font.SetBold().Font.SetFontSize(14);

        var headers = new[] { "Equipo", "# Mants", "M. Obra", "Repuestos", "Otros", "Total" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(2, i + 1).Value = headers[i];

        StyleHeader(ws.Range(2, 1, 2, headers.Length));

        int row = 3;
        foreach (var r in rows)
        {
            ws.Cell(row, 1).Value = r.EquipmentInternalCode;
            ws.Cell(row, 2).Value = r.MaintenanceCount;
            ws.Cell(row, 3).Value = r.TotalLaborCost;
            ws.Cell(row, 4).Value = r.TotalSparePartsCost;
            ws.Cell(row, 5).Value = r.TotalOtherCosts;
            ws.Cell(row, 6).Value = r.TotalCost;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.DarkGray;
        range.Style.Font.FontColor = XLColor.White;
    }
}
