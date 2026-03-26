namespace DocumentProcessor.Api.Services.Extractors;

public class ReceiptExtractor : BaseExtractor
{
    public override string DocumentType => "Receipt";
    public override string ModelId => "prebuilt-receipt";
}
