using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Dapper;
using DocumentProcessor.Api.Hubs;
using DocumentProcessor.Api.Models;
using DocumentProcessor.Api.Services.Extractors;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace DocumentProcessor.Api.Services;

public class DocumentService
{
    private readonly string _connectionString;
    private readonly BlobContainerClient _blobContainer;
    private readonly DocumentAnalysisClient _docClient;
    private readonly IHubContext<DocumentHub> _hub;
    private readonly ILogger<DocumentService> _logger;
    private readonly DocumentTypeDetector _typeDetector;
    private readonly Dictionary<string, IDocumentExtractor> _extractors;

    public DocumentService(
        IConfiguration config,
        BlobContainerClient blobContainer,
        DocumentAnalysisClient docClient,
        IHubContext<DocumentHub> hub,
        ILogger<DocumentService> logger,
        DocumentTypeDetector typeDetector,
        IEnumerable<IDocumentExtractor> extractors)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
        _blobContainer = blobContainer;
        _docClient = docClient;
        _hub = hub;
        _logger = logger;
        _typeDetector = typeDetector;
        _extractors = extractors.ToDictionary(e => e.DocumentType, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Document> UploadAndProcessAsync(Stream fileStream, string fileName, string contentType, long fileSize, string? documentType = null)
    {
        // Resolve document type
        var resolvedType = _typeDetector.Resolve(documentType, fileName);
        var extractor = _extractors.GetValueOrDefault(resolvedType) ?? _extractors["Invoice"];

        // 1. Upload to Blob Storage
        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = _blobContainer.GetBlobClient(blobName);
        await blobClient.UploadAsync(fileStream, overwrite: true);
        var blobUrl = blobClient.Uri.ToString();

        // Generate SAS URL for Document Intelligence to access the blob
        var sasUrl = blobUrl;
        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        // 2. Save document record
        using var conn = new SqlConnection(_connectionString);
        var doc = await conn.QueryFirstAsync<Document>(
            @"INSERT INTO Documents (FileName, FileType, FileSize, BlobUrl, Status, DocumentType)
              OUTPUT INSERTED.*
              VALUES (@FileName, @FileType, @FileSize, @BlobUrl, 'Uploaded', @DocumentType)",
            new { FileName = fileName, FileType = contentType, FileSize = fileSize, BlobUrl = blobUrl, DocumentType = extractor.DocumentType });

        await LogAction(conn, doc.DocumentID, "Uploaded", $"File uploaded: {fileName} ({fileSize} bytes) — Type: {extractor.DocumentType}");
        await SendProgress(doc.DocumentID, "uploaded", "File uploaded to storage");

        // 3. Start processing in background (use SAS URL so Document Intelligence can access the blob)
        _ = Task.Run(() => ProcessDocumentAsync(doc.DocumentID, sasUrl, extractor));

        return doc;
    }

    private async Task ProcessDocumentAsync(int documentId, string blobUrl, IDocumentExtractor extractor)
    {
        using var conn = new SqlConnection(_connectionString);
        try
        {
            // Update status to Processing
            await conn.ExecuteAsync(
                "UPDATE Documents SET Status = 'Processing' WHERE DocumentID = @DocumentID",
                new { DocumentID = documentId });
            await LogAction(conn, documentId, "Processing", $"AI analysis started using model: {extractor.ModelId}");
            await SendProgress(documentId, "analyzing", $"Analyzing with {extractor.ModelId}");

            // Call Azure AI Document Intelligence
            var operation = await _docClient.AnalyzeDocumentFromUriAsync(
                WaitUntil.Completed,
                extractor.ModelId,
                new Uri(blobUrl));

            var result = operation.Value;
            await SendProgress(documentId, "extracting", "Extracting fields and data");

            // Use type-specific extractors
            await extractor.ExtractFieldsAsync(conn, documentId, result);
            await extractor.ExtractLineItemsAsync(conn, documentId, result);

            // Update status to Completed
            await conn.ExecuteAsync(
                @"UPDATE Documents SET Status = 'Completed', ProcessedDate = GETUTCDATE(),
                  PageCount = @PageCount, ModelId = @ModelId
                  WHERE DocumentID = @DocumentID",
                new { DocumentID = documentId, PageCount = result.Pages.Count, ModelId = extractor.ModelId });

            var fieldCount = result.Documents.Sum(d => d.Fields.Count);
            await LogAction(conn, documentId, "Completed", $"Extracted {fieldCount} fields from {result.Pages.Count} pages");
            await SendProgress(documentId, "completed", "Processing complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document {DocumentID}", documentId);
            await conn.ExecuteAsync(
                "UPDATE Documents SET Status = 'Failed', ErrorMessage = @Error WHERE DocumentID = @DocumentID",
                new { DocumentID = documentId, Error = ex.Message });
            await LogAction(conn, documentId, "Failed", ex.Message);
            await SendProgress(documentId, "failed", ex.Message);
        }
    }

    private async Task SendProgress(int documentId, string step, string message)
    {
        await _hub.Clients.All.SendAsync("DocumentProgress", new { documentId, step, message });
        await _hub.Clients.All.SendAsync("DocumentChanged");
    }

    private static async Task LogAction(SqlConnection conn, int documentId, string action, string? details)
    {
        await conn.ExecuteAsync(
            "INSERT INTO ProcessingLog (DocumentID, Action, Details) VALUES (@DocumentID, @Action, @Details)",
            new { DocumentID = documentId, Action = action, Details = details });
    }
}
