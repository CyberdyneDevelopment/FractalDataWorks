using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by build project operation.
/// </summary>
public sealed class BuildProjectData
{
    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    /// Gets or sets whether the build succeeded.
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
    /// Gets or sets the list of errors.
    /// </summary>
    public required IReadOnlyList<CompilationDiagnosticInfo> Errors { get; init; }

    /// <summary>
    /// Gets or sets the list of warnings.
    /// </summary>
    public required IReadOnlyList<CompilationDiagnosticInfo> Warnings { get; init; }
}
