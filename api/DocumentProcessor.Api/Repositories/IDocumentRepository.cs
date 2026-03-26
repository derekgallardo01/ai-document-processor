using DocumentProcessor.Api.Models;

namespace DocumentProcessor.Api.Repositories;

public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetAllAsync(string? status = null);
    Task<Document?> GetByIdAsync(int id);
    Task<IEnumerable<ExtractedField>> GetFieldsAsync(int documentId);
    Task<IEnumerable<LineItem>> GetLineItemsAsync(int documentId);
    Task<IEnumerable<ProcessingLogEntry>> GetLogAsync(int documentId);
    Task<DashboardMetrics> GetMetricsAsync();
    Task<int> UpdateFieldAsync(int documentId, int fieldId, string value);
    Task<int> ReviewDocumentAsync(int documentId, string status, string reviewedBy, string? notes);
    Task<int> DeleteAsync(int id);
}
