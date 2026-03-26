namespace DocumentProcessor.Api.Services.Extractors;

public class InvoiceExtractor : BaseExtractor
{
    public override string DocumentType => "Invoice";
    public override string ModelId => "prebuilt-invoice";
}
