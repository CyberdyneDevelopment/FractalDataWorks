using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by compile document operation.
/// </summary>
public sealed class CompileDocumentData
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets whether compilation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or sets the error count.
    /// </summary>
    public required int ErrorCount { get; init; }

    /// <summary>
    /// Gets or sets the warning count.
    /// </summary>
    public required int WarningCount { get; init; }

    /// <summary>
    /// Gets or sets the list of diagnostics.
    /// </summary>
    public required IReadOnlyList<DiagnosticInfo> Diagnostics { get; init; }
}
