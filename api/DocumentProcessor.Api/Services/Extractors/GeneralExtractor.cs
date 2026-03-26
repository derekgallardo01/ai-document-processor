namespace DocumentProcessor.Api.Services.Extractors;

public class GeneralExtractor : BaseExtractor
{
    public override string DocumentType => "General";
    public override string ModelId => "prebuilt-layout";
}
