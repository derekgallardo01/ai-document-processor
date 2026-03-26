using DocumentProcessor.Api.Models;
using DocumentProcessor.Api.Repositories;
using DocumentProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _repository;
    private readonly DocumentService _documentService;

    public DocumentsController(IDocumentRepository repository, DocumentService documentService)
    {
        _repository = repository;
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] string? status = null)
    {
        var result = await _repository.GetAllAsync(status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        var doc = await _repository.GetByIdAsync(id);
        return doc is null ? NotFound() : Ok(doc);
    }

    [HttpGet("{id}/fields")]
    public async Task<IActionResult> GetFields(int id)
    {
        var fields = await _repository.GetFieldsAsync(id);
        return Ok(fields);
    }

    [HttpGet("{id}/lineitems")]
    public async Task<IActionResult> GetLineItems(int id)
    {
        var items = await _repository.GetLineItemsAsync(id);
        return Ok(items);
    }

    [HttpGet("{id}/log")]
    public async Task<IActionResult> GetProcessingLog(int id)
    {
        var log = await _repository.GetLogAsync(id);
        return Ok(log);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20MB
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? documentType = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/tiff" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "File type not supported. Use PDF, JPEG, PNG, or TIFF." });

        using var stream = file.OpenReadStream();
        var doc = await _documentService.UploadAndProcessAsync(stream, file.FileName, file.ContentType, file.Length, documentType);
        return CreatedAtAction(nameof(GetDocument), new { id = doc.DocumentID }, doc);
    }

    [HttpPut("{id}/fields/{fieldId}")]
    public async Task<IActionResult> UpdateField(int id, int fieldId, [FromBody] UpdateFieldRequest request)
    {
        var rows = await _repository.UpdateFieldAsync(id, fieldId, request.FieldValue);
        return rows == 0 ? NotFound() : Ok();
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> ReviewDocument(int id, [FromBody] ReviewRequest request)
    {
        if (request.Status != "Approved" && request.Status != "Rejected")
            return BadRequest(new { message = "Status must be 'Approved' or 'Rejected'" });

        var rows = await _repository.ReviewDocumentAsync(id, request.Status, request.ReviewedBy, request.Notes);
        return rows == 0 ? NotFound() : Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var rows = await _repository.DeleteAsync(id);
        return rows == 0 ? NotFound() : NoContent();
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _repository.GetMetricsAsync();
        return Ok(metrics);
    }
}
