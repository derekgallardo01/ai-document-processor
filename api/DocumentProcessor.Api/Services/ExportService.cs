using ClosedXML.Excel;
using Dapper;
using DocumentProcessor.Api.Models;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Api.Services;

public class ExportService
{
    private readonly string _connectionString;

    public ExportService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<byte[]> ExportToExcelAsync(int documentId)
    {
        using var conn = new SqlConnection(_connectionString);
        var doc = await conn.QueryFirstOrDefaultAsync<Document>(
            "SELECT * FROM Documents WHERE DocumentID = @Id", new { Id = documentId });
        if (doc == null) throw new KeyNotFoundException($"Document {documentId} not found");

        var fields = (await conn.QueryAsync<ExtractedField>(
            "SELECT * FROM ExtractedFields WHERE DocumentID = @Id ORDER BY FieldName", new { Id = documentId })).ToList();
        var lineItems = (await conn.QueryAsync<LineItem>(
            "SELECT * FROM LineItems WHERE DocumentID = @Id ORDER BY RowIndex", new { Id = documentId })).ToList();

        using var workbook = new XLWorkbook();

        // Document Info sheet
        var infoSheet = workbook.Worksheets.Add("Document Info");
        infoSheet.Cell(1, 1).Value = "Property";
        infoSheet.Cell(1, 2).Value = "Value";
        StyleHeader(infoSheet, 2);

        var infoRows = new (string, string)[]
        {
            ("File Name", doc.FileName),
            ("Document Type", doc.DocumentType),
            ("Status", doc.Status),
            ("Uploaded", doc.UploadedDate.ToString("g")),
            ("Processed", doc.ProcessedDate?.ToString("g") ?? "—"),
            ("Pages", doc.PageCount?.ToString() ?? "—"),
            ("Model", doc.ModelId ?? "—"),
        };
        for (var i = 0; i < infoRows.Length; i++)
        {
            infoSheet.Cell(i + 2, 1).Value = infoRows[i].Item1;
            infoSheet.Cell(i + 2, 2).Value = infoRows[i].Item2;
        }
        infoSheet.Columns().AdjustToContents();

        // Extracted Fields sheet
        if (fields.Count > 0)
        {
            var fieldsSheet = workbook.Worksheets.Add("Extracted Fields");
            fieldsSheet.Cell(1, 1).Value = "Field Name";
            fieldsSheet.Cell(1, 2).Value = "Value";
            fieldsSheet.Cell(1, 3).Value = "Confidence";
            fieldsSheet.Cell(1, 4).Value = "Type";
            fieldsSheet.Cell(1, 5).Value = "Edited";
            StyleHeader(fieldsSheet, 5);

            for (var i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                fieldsSheet.Cell(i + 2, 1).Value = f.FieldName;
                fieldsSheet.Cell(i + 2, 2).Value = f.FieldValue ?? "";
                fieldsSheet.Cell(i + 2, 3).Value = f.Confidence.HasValue ? Math.Round(f.Confidence.Value * 100, 1) : 0;
                fieldsSheet.Cell(i + 2, 3).Style.NumberFormat.Format = "0.0\"%\"";
                fieldsSheet.Cell(i + 2, 4).Value = f.FieldType ?? "";
                fieldsSheet.Cell(i + 2, 5).Value = f.IsManuallyEdited ? "Yes" : "";
            }
            fieldsSheet.Columns().AdjustToContents();
        }

        // Line Items sheet
        if (lineItems.Count > 0)
        {
            var itemsSheet = workbook.Worksheets.Add("Line Items");
            itemsSheet.Cell(1, 1).Value = "#";
            itemsSheet.Cell(1, 2).Value = "Description";
            itemsSheet.Cell(1, 3).Value = "Quantity";
            itemsSheet.Cell(1, 4).Value = "Unit Price";
            itemsSheet.Cell(1, 5).Value = "Amount";
            StyleHeader(itemsSheet, 5);

            for (var i = 0; i < lineItems.Count; i++)
            {
                var li = lineItems[i];
                itemsSheet.Cell(i + 2, 1).Value = li.RowIndex + 1;
                itemsSheet.Cell(i + 2, 2).Value = li.Description ?? "";
                if (li.Quantity.HasValue) itemsSheet.Cell(i + 2, 3).Value = li.Quantity.Value;
                if (li.UnitPrice.HasValue) { itemsSheet.Cell(i + 2, 4).Value = li.UnitPrice.Value; itemsSheet.Cell(i + 2, 4).Style.NumberFormat.Format = "$#,##0.00"; }
                if (li.Amount.HasValue) { itemsSheet.Cell(i + 2, 5).Value = li.Amount.Value; itemsSheet.Cell(i + 2, 5).Style.NumberFormat.Format = "$#,##0.00"; }
            }
            itemsSheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<string> ExportToCsvAsync(int documentId)
    {
        using var conn = new SqlConnection(_connectionString);
        var fields = await conn.QueryAsync<ExtractedField>(
            "SELECT * FROM ExtractedFields WHERE DocumentID = @Id ORDER BY FieldName", new { Id = documentId });

        var lines = new List<string> { "Field Name,Value,Confidence,Type,Edited" };
        foreach (var f in fields)
        {
            var value = EscapeCsv(f.FieldValue ?? "");
            var conf = f.Confidence.HasValue ? $"{Math.Round(f.Confidence.Value * 100, 1)}%" : "";
            lines.Add($"{EscapeCsv(f.FieldName)},{value},{conf},{f.FieldType ?? ""},{(f.IsManuallyEdited ? "Yes" : "")}");
        }
        return string.Join("\n", lines);
    }

    public async Task<byte[]> ExportBatchToExcelAsync(int[] documentIds)
    {
        using var conn = new SqlConnection(_connectionString);
        using var workbook = new XLWorkbook();

        foreach (var id in documentIds)
        {
            var doc = await conn.QueryFirstOrDefaultAsync<Document>(
                "SELECT * FROM Documents WHERE DocumentID = @Id", new { Id = id });
            if (doc == null) continue;

            var fields = await conn.QueryAsync<ExtractedField>(
                "SELECT * FROM ExtractedFields WHERE DocumentID = @Id ORDER BY FieldName", new { Id = id });

            var sheetName = $"Doc {id}";
            if (sheetName.Length > 31) sheetName = sheetName[..31];
            var sheet = workbook.Worksheets.Add(sheetName);

            sheet.Cell(1, 1).Value = "Field Name";
            sheet.Cell(1, 2).Value = "Value";
            sheet.Cell(1, 3).Value = "Confidence";
            StyleHeader(sheet, 3);

            var row = 2;
            // Add doc info
            sheet.Cell(row, 1).Value = "File Name";
            sheet.Cell(row, 2).Value = doc.FileName;
            row++;
            sheet.Cell(row, 1).Value = "Document Type";
            sheet.Cell(row, 2).Value = doc.DocumentType;
            row++;
            sheet.Cell(row, 1).Value = "---";
            row++;

            foreach (var f in fields)
            {
                sheet.Cell(row, 1).Value = f.FieldName;
                sheet.Cell(row, 2).Value = f.FieldValue ?? "";
                sheet.Cell(row, 3).Value = f.Confidence.HasValue ? Math.Round(f.Confidence.Value * 100, 1) : 0;
                row++;
            }
            sheet.Columns().AdjustToContents();
        }

        if (workbook.Worksheets.Count == 0)
            workbook.Worksheets.Add("Empty");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void StyleHeader(IXLWorksheet sheet, int colCount)
    {
        var headerRange = sheet.Range(1, 1, 1, colCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
