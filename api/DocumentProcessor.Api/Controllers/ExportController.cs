using DocumentProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocumentProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly ExportService _exportService;

    public ExportController(ExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("{id}/excel")]
    public async Task<IActionResult> ExportExcel(int id)
    {
        var bytes = await _exportService.ExportToExcelAsync(id);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"document-{id}.xlsx");
    }

    [HttpGet("{id}/csv")]
    public async Task<IActionResult> ExportCsv(int id)
    {
        var csv = await _exportService.ExportToCsvAsync(id);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"document-{id}.csv");
    }

    [HttpPost("batch/excel")]
    public async Task<IActionResult> ExportBatchExcel([FromBody] int[] documentIds)
    {
        if (documentIds == null || documentIds.Length == 0)
            return BadRequest(new { message = "No document IDs provided" });

        var bytes = await _exportService.ExportBatchToExcelAsync(documentIds);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "documents-export.xlsx");
    }
}
