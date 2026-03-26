using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Api.Services.Extractors;

public interface IDocumentExtractor
{
    string DocumentType { get; }
    string ModelId { get; }
    Task ExtractFieldsAsync(SqlConnection conn, int documentId, AnalyzeResult result);
    Task ExtractLineItemsAsync(SqlConnection conn, int documentId, AnalyzeResult result);
}
