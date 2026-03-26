using DocumentProcessor.Api.Services;
using FluentAssertions;

namespace DocumentProcessor.Tests.Services;

public class DocumentTypeDetectorTests
{
    private readonly DocumentTypeDetector _detector = new();

    [Theory]
    [InlineData("invoice-2024.pdf", "Invoice")]
    [InlineData("billing_statement.pdf", "Invoice")]
    [InlineData("Receipt_Walmart.jpg", "Receipt")]
    [InlineData("purchase_receipt.pdf", "Receipt")]
    [InlineData("W2-2024-employer.pdf", "W2")]
    [InlineData("tax_form_w-2.pdf", "W2")]
    [InlineData("business_card_scan.png", "BusinessCard")]
    [InlineData("contact-card.jpg", "BusinessCard")]
    public void Detect_WithHintInFileName_ReturnsCorrectType(string fileName, string expected)
    {
        _detector.Detect(fileName).Should().Be(expected);
    }

    [Theory]
    [InlineData("random-document.pdf")]
    [InlineData("scan001.jpg")]
    [InlineData("photo.png")]
    public void Detect_WithNoHint_ReturnsAuto(string fileName)
    {
        _detector.Detect(fileName).Should().Be("Auto");
    }

    [Theory]
    [InlineData("Receipt", "anything.pdf", "Receipt")]
    [InlineData("W2", "anything.pdf", "W2")]
    [InlineData(null, "invoice-123.pdf", "Invoice")]
    [InlineData("Auto", "invoice-123.pdf", "Invoice")]
    [InlineData(null, "random.pdf", "Invoice")]
    [InlineData("Auto", "random.pdf", "Invoice")]
    public void Resolve_ReturnsCorrectType(string? userSelected, string fileName, string expected)
    {
        _detector.Resolve(userSelected, fileName).Should().Be(expected);
    }
}
