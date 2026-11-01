using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// The outcome of a <c>RemoveGlobalUsings</c> run.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RemoveGlobalUsingsData
{
    /// <summary>Gets or sets the project the directives were removed from.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Gets or sets the global using directives that were removed.</summary>
    public IReadOnlyList<string> Removed { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the files that gained an explicit using, with the using they gained.</summary>
    public IReadOnlyList<string> Repaired { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the diagnostics that DISAPPEARED because a directive was removed.
    /// </summary>
    /// <remarks>
    /// Reported because removing an import can FIX code — a global using that made two same-named types
    /// visible at once is the cause of a CS0104, not a casualty of removing it. A command that only ever
    /// reported new breakage would hide the reason you wanted the removal.
    /// </remarks>
    public IReadOnlyList<string> Resolved { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the files left holding no directives at all.
    /// </summary>
    /// <remarks>
    /// Reported rather than deleted. Deleting a file is a separate, riskier act than editing one, and the
    /// caller may want the file kept as the obvious home for a future import.
    /// </remarks>
    public IReadOnlyList<string> EmptiedFiles { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets whether this was a preview.</summary>
    public bool WasDryRun { get; set; }

    /// <summary>Gets or sets the number of files that needed no change.</summary>
    public int FilesUnaffected { get; set; }

    /// <summary>Gets or sets the paths of the changed documents.</summary>
    public IReadOnlyList<string> AffectedFiles { get; set; } = Array.Empty<string>();
}
