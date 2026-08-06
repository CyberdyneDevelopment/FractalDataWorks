using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by emit assembly operation.
/// </summary>
public sealed class EmitAssemblyData
{
    /// <summary>
    /// Gets or sets whether emit succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets or sets the output assembly path.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets or sets the PDB path (if emitted).
    /// </summary>
    public string? PdbPath { get; init; }

    /// <summary>
    /// Gets or sets the list of errors (if any).
    /// </summary>
    public IReadOnlyList<DiagnosticInfo>? Errors { get; init; }
}
