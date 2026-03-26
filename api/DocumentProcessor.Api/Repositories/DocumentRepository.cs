using Dapper;
using DocumentProcessor.Api.Models;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Api.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly string _connectionString;

    public DocumentRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task<IEnumerable<Document>> GetAllAsync(string? status = null)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = status != null
            ? "SELECT * FROM Documents WHERE Status = @Status ORDER BY UploadedDate DESC"
            : "SELECT * FROM Documents ORDER BY UploadedDate DESC";
        return await conn.QueryAsync<Document>(sql, new { Status = status });
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Document>(
            "SELECT * FROM Documents WHERE DocumentID = @Id", new { Id = id });
    }

    public async Task<IEnumerable<ExtractedField>> GetFieldsAsync(int documentId)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<ExtractedField>(
            "SELECT * FROM ExtractedFields WHERE DocumentID = @Id ORDER BY FieldName", new { Id = documentId });
    }

    public async Task<IEnumerable<LineItem>> GetLineItemsAsync(int documentId)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<LineItem>(
            "SELECT * FROM LineItems WHERE DocumentID = @Id ORDER BY RowIndex", new { Id = documentId });
    }

    public async Task<IEnumerable<ProcessingLogEntry>> GetLogAsync(int documentId)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<ProcessingLogEntry>(
            "SELECT * FROM ProcessingLog WHERE DocumentID = @Id ORDER BY CreatedDate DESC", new { Id = documentId });
    }

    public async Task<DashboardMetrics> GetMetricsAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstAsync<DashboardMetrics>(
            @"SELECT
                COUNT(*) AS TotalDocuments,
                SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS ProcessedCount,
                SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) AS FailedCount,
                SUM(CASE WHEN Status IN ('Uploaded', 'Processing') THEN 1 ELSE 0 END) AS PendingCount,
                ISNULL(AVG(f.Confidence), 0) AS AvgConfidence,
                (SELECT COUNT(*) FROM ExtractedFields) AS TotalFieldsExtracted,
                (SELECT COUNT(*) FROM ExtractedFields WHERE IsManuallyEdited = 1) AS ManualCorrections
              FROM Documents d
              LEFT JOIN ExtractedFields f ON d.DocumentID = f.DocumentID");
    }

    public async Task<int> UpdateFieldAsync(int documentId, int fieldId, string value)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteAsync(
            @"UPDATE ExtractedFields SET FieldValue = @Value, IsManuallyEdited = 1
              WHERE FieldID = @FieldId AND DocumentID = @DocumentId",
            new { Value = value, FieldId = fieldId, DocumentId = documentId });
    }

    public async Task<int> ReviewDocumentAsync(int documentId, string status, string reviewedBy, string? notes)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteAsync(
            @"UPDATE Documents SET ReviewStatus = @Status, ReviewedBy = @ReviewedBy,
              ReviewedDate = GETUTCDATE(), ReviewNotes = @Notes
              WHERE DocumentID = @Id AND Status = 'Completed'",
            new { Id = documentId, Status = status, ReviewedBy = reviewedBy, Notes = notes });
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteAsync("DELETE FROM Documents WHERE DocumentID = @Id", new { Id = id });
    }
}
