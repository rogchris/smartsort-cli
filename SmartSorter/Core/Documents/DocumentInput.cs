namespace SmartSort.App.Core.Documents;

/// <summary>
/// A document name and a lazy content source, independent of local storage.
/// This is an input contract, not a persisted document entity.
/// </summary>
public sealed class DocumentInput
{
    private readonly Func<CancellationToken, Task<Stream>> _openReadAsync;

    public DocumentInput(string originalFileName, Func<CancellationToken, Task<Stream>> openReadAsync)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        if (originalFileName is "." or ".." || originalFileName.Any(char.IsControl) || originalFileName.IndexOfAny(['/', '\\', ':', '\0']) >= 0)
            throw new ArgumentException("A document name must be a file name, not a path.", nameof(originalFileName));

        OriginalFileName = originalFileName;
        Extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        _openReadAsync = openReadAsync ?? throw new ArgumentNullException(nameof(openReadAsync));
    }

    public string OriginalFileName { get; }

    /// <summary>An untrusted filename suffix; this does not establish the actual file format.</summary>
    public string Extension { get; }

    /// <summary>Opens a fresh readable stream. The caller owns and must dispose it.</summary>
    public async Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = await _openReadAsync(cancellationToken).ConfigureAwait(false);
        if (stream is null)
            throw new InvalidOperationException("The content source returned no stream.");
        if (cancellationToken.IsCancellationRequested || !stream.CanRead)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("The content source returned an unreadable stream.");
        }
        return stream;
    }
}
