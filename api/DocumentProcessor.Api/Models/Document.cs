using System.ComponentModel.DataAnnotations;

namespace DocumentProcessor.Api.Models;

public class Document
{
    public int DocumentID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Uploaded";
    public string DocumentType { get; set; } = "Invoice";
    public DateTime UploadedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public int? PageCount { get; set; }
    public string? ModelId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ReviewStatus { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewNotes { get; set; }
}

public class ReviewRequest
{
    public string Status { get; set; } = string.Empty; // Approved, Rejected
    public string? Notes { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
}

public class ExtractedField
{
    public int FieldID { get; set; }
    public int DocumentID { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
    public decimal? Confidence { get; set; }
    public string? FieldType { get; set; }
    public int? PageNumber { get; set; }
    public bool IsManuallyEdited { get; set; }
}

public class LineItem
{
    public int LineItemID { get; set; }
    public int DocumentID { get; set; }
    public int RowIndex { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Confidence { get; set; }
}

public class ProcessingLogEntry
{
    public int LogID { get; set; }
    public int DocumentID { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class UpdateFieldRequest
{
    [Required]
    public string FieldValue { get; set; } = string.Empty;
}

public class DashboardMetrics
{
    public int TotalDocuments { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal AvgConfidence { get; set; }
    public int TotalFieldsExtracted { get; set; }
    public int ManualCorrections { get; set; }
}
