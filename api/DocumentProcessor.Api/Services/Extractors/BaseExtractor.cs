using Azure.AI.FormRecognizer.DocumentAnalysis;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Api.Services.Extractors;

public abstract class BaseExtractor : IDocumentExtractor
{
    public abstract string DocumentType { get; }
    public abstract string ModelId { get; }

    public virtual async Task ExtractFieldsAsync(SqlConnection conn, int documentId, AnalyzeResult result)
    {
        foreach (var document in result.Documents)
        {
            foreach (var field in document.Fields)
            {
                // Skip list/array fields (e.g. "Items") — these are handled by ExtractLineItemsAsync
                if (field.Value.FieldType == DocumentFieldType.List)
                    continue;

                var confidence = field.Value.Confidence;
                var value = field.Value.Content ?? field.Value.ToString();
                var fieldType = field.Value.FieldType.ToString();
                var displayName = FormatFieldName(field.Key);

                await conn.ExecuteAsync(
                    @"INSERT INTO ExtractedFields (DocumentID, FieldName, FieldValue, Confidence, FieldType, PageNumber)
                      VALUES (@DocumentID, @FieldName, @FieldValue, @Confidence, @FieldType, 1)",
                    new { DocumentID = documentId, FieldName = displayName, FieldValue = value, Confidence = confidence, FieldType = fieldType });
            }
        }
    }

    public virtual async Task ExtractLineItemsAsync(SqlConnection conn, int documentId, AnalyzeResult result)
    {
        var rowIndex = 0;
        foreach (var table in result.Tables)
        {
            for (var r = 1; r < table.RowCount; r++)
            {
                var cells = table.Cells.Where(c => c.RowIndex == r).OrderBy(c => c.ColumnIndex).ToList();
                if (cells.Count >= 2)
                {
                    var desc = cells.FirstOrDefault()?.Content;
                    var amountStr = cells.LastOrDefault()?.Content;
                    decimal.TryParse(amountStr?.Replace("$", "").Replace(",", ""), out var amount);

                    await conn.ExecuteAsync(
                        @"INSERT INTO LineItems (DocumentID, RowIndex, Description, Amount, Confidence)
                          VALUES (@DocumentID, @RowIndex, @Description, @Amount, @Confidence)",
                        new { DocumentID = documentId, RowIndex = rowIndex++, Description = desc, Amount = amount, Confidence = 0.9m });
                }
            }
        }
    }

    protected static string FormatFieldName(string key)
    {
        // Convert camelCase/PascalCase to readable: "VendorName" -> "Vendor Name"
        var result = System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
        result = System.Text.RegularExpressions.Regex.Replace(result, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return result;
    }
}
