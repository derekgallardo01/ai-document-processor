namespace DocumentProcessor.Api.Services.Extractors;

public class BusinessCardExtractor : BaseExtractor
{
    public override string DocumentType => "BusinessCard";
    public override string ModelId => "prebuilt-businessCard";

    // Business cards don't have line items
    public override Task ExtractLineItemsAsync(
        Microsoft.Data.SqlClient.SqlConnection conn,
        int documentId,
        Azure.AI.FormRecognizer.DocumentAnalysis.AnalyzeResult result)
        => Task.CompletedTask;
}
