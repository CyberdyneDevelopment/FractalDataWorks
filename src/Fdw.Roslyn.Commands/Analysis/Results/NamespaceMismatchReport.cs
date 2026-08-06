using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// The result of <c>FindNamespaceMismatches</c>: every type whose namespace disagrees with where it
/// physically lives, grouped by cause.
/// </summary>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class NamespaceMismatchReport
{
    /// <summary>Gets or sets the total number of mismatched types found.</summary>
    public int TotalMismatches { get; set; }

    /// <summary>Gets or sets the number of types scanned.</summary>
    public int TypesScanned { get; set; }

    /// <summary>Gets or sets the number of distinct causes.</summary>
    public int GroupCount { get; set; }

    /// <summary>Gets or sets the number of mismatched types carrying a TypeOption attribute.</summary>
    public int TypeOptionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of groups whose namespace has no project of its own — each needs a new
    /// project or a namespace rename, not a plain move.
    /// </summary>
    public int GroupsWithoutTargetProject { get; set; }

    /// <summary>Gets or sets whether test projects were included in the scan.</summary>
    public bool IncludedTests { get; set; }

    /// <summary>Gets or sets the mismatch groups, largest first.</summary>
    public IReadOnlyList<NamespaceMismatchGroup> Groups { get; set; } = Array.Empty<NamespaceMismatchGroup>();
}
