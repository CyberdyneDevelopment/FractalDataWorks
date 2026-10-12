namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents diagnostic information.
/// </summary>
public sealed class DiagnosticInfo
{
    /// <summary>
    /// Gets or sets the diagnostic ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the diagnostic message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number.
    /// </summary>
    public required int Column { get; init; }
}