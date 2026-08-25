using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by validate syntax operation.
/// </summary>
public sealed class ValidateSyntaxData
{
    /// <summary>
    /// Gets or sets whether the syntax is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public required int ErrorCount { get; init; }

    /// <summary>
    /// Gets or sets the list of syntax errors.
    /// </summary>
    public required IReadOnlyList<CompilationDiagnosticInfo> Errors { get; init; }

    /// <summary>
    /// Gets or sets the file path (if validating a file).
    /// </summary>
    public string? FilePath { get; init; }
}
