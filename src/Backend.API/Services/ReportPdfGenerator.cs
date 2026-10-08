using Backend.API.DTOs;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace Backend.API.Services;

public class ReportPdfGenerator : IReportPdfGenerator
{
    public byte[] GenerateEquipmentsPdf(string title, List<EquipmentReportRow> rows, ReportFilter filter)
    {
        var document = CreateDocument(title, filter, landscape: true);

        var table = document.LastSection.AddTable();
        table.Borders.Visible = true;
        table.Format.Font.Size = 8;

        table.AddColumn("2.5cm"); // Código
        table.AddColumn("2.5cm"); // Serie
        table.AddColumn("2cm");   // Tipo
        table.AddColumn("2.5cm"); // Marca
        table.AddColumn("2.5cm"); // Modelo
        table.AddColumn("2.5cm"); // Ubicación
        table.AddColumn("2cm");   // Estado
        table.AddColumn("2cm");   // Precio
        table.AddColumn("3cm");   // Asignado

        var headerRow = table.AddRow();
        headerRow.Shading.Color = Colors.DarkGray;
        headerRow.Format.Font.Bold = true;
        headerRow.Format.Font.Color = Colors.White;
        headerRow.HeadingFormat = true;

        var headers = new[] { "Código", "Serie", "Tipo", "Marca", "Modelo", "Ubicación", "Estado", "Precio", "Asignado a" };
        for (int i = 0; i < headers.Length; i++)
            headerRow.Cells[i].AddParagraph(headers[i]);

        foreach (var r in rows)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(r.InternalCode);
            row.Cells[1].AddParagraph(r.SerialNumber);
            row.Cells[2].AddParagraph(r.Type);
            row.Cells[3].AddParagraph(r.Brand);
            row.Cells[4].AddParagraph(r.Model);
            row.Cells[5].AddParagraph(r.Location);
            row.Cells[6].AddParagraph(r.Status);
            row.Cells[7].AddParagraph(r.AcquisitionPrice?.ToString("C") ?? "—");
            row.Cells[8].AddParagraph(r.AssignedTo ?? "—");
        }

        return Render(document);
    }

    public byte[] GenerateMaintenancesPdf(string title, List<MaintenanceReportRow> rows, ReportFilter filter)
    {
        var document = CreateDocument(title, filter, landscape: true);

        var table = document.LastSection.AddTable();
        table.Borders.Visible = true;
        table.Format.Font.Size = 8;

        table.AddColumn("2.5cm"); // Equipo
        table.AddColumn("2cm");   // Tipo
        table.AddColumn("2cm");   // Estado
        table.AddColumn("3cm");   // Técnico
        table.AddColumn("2.5cm"); // Inicio
        table.AddColumn("2.5cm"); // Fin
        table.AddColumn("2cm");   // M. Obra
        table.AddColumn("2cm");   // Otros
        table.AddColumn("2cm");   // Repuestos
        table.AddColumn("2cm");   // Total

        var headerRow = table.AddRow();
        headerRow.Shading.Color = Colors.DarkGray;
        headerRow.Format.Font.Bold = true;
        headerRow.Format.Font.Color = Colors.White;
        headerRow.HeadingFormat = true;

        var headers = new[] { "Equipo", "Tipo", "Estado", "Técnico", "Inicio", "Fin", "M. Obra", "Otros", "Repuestos", "Total" };
        for (int i = 0; i < headers.Length; i++)
            headerRow.Cells[i].AddParagraph(headers[i]);

        foreach (var r in rows)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(r.EquipmentInternalCode);
            row.Cells[1].AddParagraph(r.Type);
            row.Cells[2].AddParagraph(r.Status);
            row.Cells[3].AddParagraph(r.TechnicianName);
            row.Cells[4].AddParagraph(r.StartedAt.ToString("yyyy-MM-dd"));
            row.Cells[5].AddParagraph(r.CompletedAt?.ToString("yyyy-MM-dd") ?? "—");
            row.Cells[6].AddParagraph(r.LaborCost.ToString("C"));
            row.Cells[7].AddParagraph(r.OtherCosts.ToString("C"));
            row.Cells[8].AddParagraph(r.SparePartsCost.ToString("C"));
            row.Cells[9].AddParagraph(r.TotalCost.ToString("C"));
        }

        return Render(document);
    }

    public byte[] GenerateCostsPdf(string title, List<CostReportRow> rows, ReportFilter filter)
    {
        var document = CreateDocument(title, filter, landscape: false);

        var table = document.LastSection.AddTable();
        table.Borders.Visible = true;
        table.Format.Font.Size = 9;

        table.AddColumn("4cm");  // Equipo
        table.AddColumn("2cm");  // # Mants
        table.AddColumn("2.5cm"); // M. Obra
        table.AddColumn("2.5cm"); // Repuestos
        table.AddColumn("2.5cm"); // Otros
        table.AddColumn("2.5cm"); // Total

        var headerRow = table.AddRow();
        headerRow.Shading.Color = Colors.DarkGray;
        headerRow.Format.Font.Bold = true;
        headerRow.Format.Font.Color = Colors.White;
        headerRow.HeadingFormat = true;

        var headers = new[] { "Equipo", "# Mants", "M. Obra", "Repuestos", "Otros", "Total" };
        for (int i = 0; i < headers.Length; i++)
            headerRow.Cells[i].AddParagraph(headers[i]);

        foreach (var r in rows)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(r.EquipmentInternalCode);
            row.Cells[1].AddParagraph(r.MaintenanceCount.ToString());
            row.Cells[2].AddParagraph(r.TotalLaborCost.ToString("C"));
            row.Cells[3].AddParagraph(r.TotalSparePartsCost.ToString("C"));
            row.Cells[4].AddParagraph(r.TotalOtherCosts.ToString("C"));
            row.Cells[5].AddParagraph(r.TotalCost.ToString("C"));
        }

        return Render(document);
    }

    private static Document CreateDocument(string title, ReportFilter filter, bool landscape)
    {
        var document = new Document();
        document.Info.Title = title;

        var section = document.AddSection();
        section.PageSetup = document.DefaultPageSetup.Clone();
        if (landscape)
        {
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.PageFormat = PageFormat.A4;
        }

        // Header
        var header = section.Headers.Primary;
        header.AddParagraph(title).Format.Font.Size = 16;
        header.AddParagraph($"Generado: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").Format.Font.Size = 8;

        var filterParts = new List<string>();
        if (filter.From.HasValue) filterParts.Add($"Desde {filter.From:yyyy-MM-dd}");
        if (filter.To.HasValue) filterParts.Add($"Hasta {filter.To:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(filter.Type)) filterParts.Add($"Tipo: {filter.Type}");
        if (!string.IsNullOrWhiteSpace(filter.Status)) filterParts.Add($"Estado: {filter.Status}");
        if (filterParts.Any())
            header.AddParagraph(string.Join(" · ", filterParts)).Format.Font.Size = 8;

        // Footer con paginación
        var footer = section.Footers.Primary;
        var footerParagraph = footer.AddParagraph();
        footerParagraph.Format.Alignment = ParagraphAlignment.Center;
        footerParagraph.AddText("Sistema de Mantenimiento · Página ");
        footerParagraph.AddPageField();
        footerParagraph.AddText(" de ");
        footerParagraph.AddNumPagesField();
        footerParagraph.Format.Font.Size = 8;
        footerParagraph.Format.Font.Color = Colors.Gray;

        return document;
    }

    private static byte[] Render(Document document)
    {
        var renderer = new PdfDocumentRenderer();
        renderer.Document = document;
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }
}
