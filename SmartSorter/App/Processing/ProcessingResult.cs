using SmartSort.App.Core;

namespace SmartSort.App.Processing;

public enum ProcessingOutcome { Simulated, Moved, Failed }

/// <summary>Current processing facts suitable for a future history adapter, without document contents.</summary>
public sealed record ProcessingResult(string SourcePath, ClassificationResult? Classification,
    ProcessingOutcome Outcome, string? DestinationPath = null, string? ErrorType = null);

public sealed record ProcessingSummary(int Discovered, int Moved, int Simulated, int Failed)
{
    public int ExitCode => Failed > 0 ? 1 : 0;
}
