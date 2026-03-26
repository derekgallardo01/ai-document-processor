namespace DocumentProcessor.Api.Services;

public class DocumentTypeDetector
{
    private static readonly Dictionary<string, string[]> TypeHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Invoice"] = ["invoice", "inv", "bill", "billing"],
        ["Receipt"] = ["receipt", "rcpt", "purchase"],
        ["W2"] = ["w2", "w-2", "tax", "wage"],
        ["BusinessCard"] = ["card", "contact", "vcard", "business"],
    };

    /// <summary>
    /// Detects document type from the file name.
    /// Returns "Auto" if no clear match, letting the service use prebuilt-invoice as default.
    /// </summary>
    public string Detect(string fileName)
    {
        var lower = fileName.ToLowerInvariant();

        foreach (var (type, hints) in TypeHints)
        {
            if (hints.Any(h => lower.Contains(h)))
                return type;
        }

        return "Auto";
    }

    /// <summary>
    /// Returns the resolved document type. If the user selected a specific type, use it.
    /// If "Auto", try to detect from file name, defaulting to "Invoice".
    /// </summary>
    public string Resolve(string? userSelected, string fileName)
    {
        if (!string.IsNullOrEmpty(userSelected) && userSelected != "Auto")
            return userSelected;

        var detected = Detect(fileName);
        return detected == "Auto" ? "Invoice" : detected;
    }
}
