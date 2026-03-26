using Azure.AI.FormRecognizer.DocumentAnalysis;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Api.Services.Extractors;

public class W2Extractor : BaseExtractor
{
    public override string DocumentType => "W2";
    public override string ModelId => "prebuilt-tax.us.w2";

    public override async Task ExtractFieldsAsync(SqlConnection conn, int documentId, AnalyzeResult result)
    {
        foreach (var document in result.Documents)
        {
            foreach (var field in document.Fields)
            {
                var confidence = field.Value.Confidence;
                var value = field.Value.Content ?? field.Value.ToString();
                var fieldType = field.Value.FieldType.ToString();
                var displayName = FormatFieldName(field.Key);

                // Mask SSN for security
                if (field.Key.Contains("SSN", StringComparison.OrdinalIgnoreCase) ||
                    field.Key.Contains("SocialSecurity", StringComparison.OrdinalIgnoreCase))
                {
                    if (value?.Length >= 4)
                        value = "***-**-" + value[^4..];
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO ExtractedFields (DocumentID, FieldName, FieldValue, Confidence, FieldType, PageNumber)
                      VALUES (@DocumentID, @FieldName, @FieldValue, @Confidence, @FieldType, 1)",
                    new { DocumentID = documentId, FieldName = displayName, FieldValue = value, Confidence = confidence, FieldType = fieldType });
            }
        }
    }

    // W-2 forms typically don't have line item tables
    public override Task ExtractLineItemsAsync(SqlConnection conn, int documentId, AnalyzeResult result)
        => Task.CompletedTask;
}
