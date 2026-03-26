using DocumentProcessor.Api.Controllers;
using DocumentProcessor.Api.Models;
using DocumentProcessor.Api.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace DocumentProcessor.Tests.Controllers;

public class DocumentsControllerTests
{
    private readonly IDocumentRepository _repo;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _repo = Substitute.For<IDocumentRepository>();
        _controller = new DocumentsController(_repo, null!);
    }

    [Fact]
    public async Task GetDocuments_ReturnsOkWithDocuments()
    {
        var docs = new[] { new Document { DocumentID = 1, FileName = "test.pdf" } };
        _repo.GetAllAsync(null).Returns(docs);

        var result = await _controller.GetDocuments();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(docs);
    }

    [Fact]
    public async Task GetDocuments_WithStatusFilter_PassesFilter()
    {
        var docs = new[] { new Document { DocumentID = 1, Status = "Completed" } };
        _repo.GetAllAsync("Completed").Returns(docs);

        var result = await _controller.GetDocuments("Completed");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(docs);
    }

    [Fact]
    public async Task GetDocument_WhenExists_ReturnsOk()
    {
        var doc = new Document { DocumentID = 1, FileName = "test.pdf" };
        _repo.GetByIdAsync(1).Returns(doc);

        var result = await _controller.GetDocument(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(doc);
    }

    [Fact]
    public async Task GetDocument_WhenNotFound_Returns404()
    {
        _repo.GetByIdAsync(999).Returns((Document?)null);

        var result = await _controller.GetDocument(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetFields_ReturnsOk()
    {
        var fields = new[] { new ExtractedField { FieldID = 1, FieldName = "Total" } };
        _repo.GetFieldsAsync(1).Returns(fields);

        var result = await _controller.GetFields(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(fields);
    }

    [Fact]
    public async Task GetLineItems_ReturnsOk()
    {
        var items = new[] { new LineItem { LineItemID = 1, Description = "Widget" } };
        _repo.GetLineItemsAsync(1).Returns(items);

        var result = await _controller.GetLineItems(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(items);
    }

    [Fact]
    public async Task GetProcessingLog_ReturnsOk()
    {
        var log = new[] { new ProcessingLogEntry { LogID = 1, Action = "Uploaded" } };
        _repo.GetLogAsync(1).Returns(log);

        var result = await _controller.GetProcessingLog(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(log);
    }

    [Fact]
    public async Task GetMetrics_ReturnsOk()
    {
        var metrics = new DashboardMetrics { TotalDocuments = 5, ProcessedCount = 3 };
        _repo.GetMetricsAsync().Returns(metrics);

        var result = await _controller.GetMetrics();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(metrics);
    }

    [Fact]
    public async Task Upload_WithNoFile_ReturnsBadRequest()
    {
        var result = await _controller.Upload(null!);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_WithInvalidContentType_ReturnsBadRequest()
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(100);
        file.ContentType.Returns("application/zip");

        var result = await _controller.Upload(file);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateField_WhenFound_ReturnsOk()
    {
        _repo.UpdateFieldAsync(1, 10, "new value").Returns(1);

        var result = await _controller.UpdateField(1, 10, new UpdateFieldRequest { FieldValue = "new value" });

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateField_WhenNotFound_Returns404()
    {
        _repo.UpdateFieldAsync(1, 999, "val").Returns(0);

        var result = await _controller.UpdateField(1, 999, new UpdateFieldRequest { FieldValue = "val" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ReviewDocument_WithApproved_ReturnsOk()
    {
        _repo.ReviewDocumentAsync(1, "Approved", "Derek", "looks good").Returns(1);

        var result = await _controller.ReviewDocument(1,
            new ReviewRequest { Status = "Approved", ReviewedBy = "Derek", Notes = "looks good" });

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ReviewDocument_WithInvalidStatus_ReturnsBadRequest()
    {
        var result = await _controller.ReviewDocument(1,
            new ReviewRequest { Status = "Invalid", ReviewedBy = "Derek" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReviewDocument_WhenNotFound_Returns404()
    {
        _repo.ReviewDocumentAsync(1, "Approved", "Derek", null).Returns(0);

        var result = await _controller.ReviewDocument(1,
            new ReviewRequest { Status = "Approved", ReviewedBy = "Derek" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteDocument_WhenExists_ReturnsNoContent()
    {
        _repo.DeleteAsync(1).Returns(1);

        var result = await _controller.DeleteDocument(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDocument_WhenNotFound_Returns404()
    {
        _repo.DeleteAsync(999).Returns(0);

        var result = await _controller.DeleteDocument(999);

        result.Should().BeOfType<NotFoundResult>();
    }
}
