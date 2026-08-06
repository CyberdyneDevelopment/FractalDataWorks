using System;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Represents a diagnostic message.
/// </summary>
public sealed class DiagnosticInfo
{
    /// <summary>
    /// Gets or sets the diagnostic ID (e.g., CS0103).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the diagnostic message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the severity (Hidden/Info/Warning/Error).
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// Gets or sets the diagnostic category.
    /// </summary>
    public string? Category { get; init; }
}
