namespace SmartSort.App.Core.Extraction;

public enum TextExtractionStatus
{
    NotRequested,
    Unsupported,
    NoText,
    Failed,
    Succeeded
}

/// <summary>Text is transient processing data and must not be logged or stored in classification results.</summary>
public sealed record TextExtractionResult(TextExtractionStatus Status, string Text = "", bool IsTruncated = false)
{
    public static TextExtractionResult FromText(string text, bool isTruncated = false) =>
        new(string.IsNullOrWhiteSpace(text) ? TextExtractionStatus.NoText : TextExtractionStatus.Succeeded,
            text, isTruncated);

    public override string ToString() => $"{Status} (Truncated={IsTruncated})";
}
