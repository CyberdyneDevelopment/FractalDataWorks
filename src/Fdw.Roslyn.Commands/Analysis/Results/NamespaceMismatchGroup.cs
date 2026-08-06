using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// A set of mismatches sharing one cause — the same namespace moving out of the same project for the
/// same reason.
/// </summary>
/// <remarks>
/// Grouping is the point of the report. 56 files sharing one cause are ONE decision; listing them as 56
/// rows buries it.
/// </remarks>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class NamespaceMismatchGroup
{
    /// <summary>Gets or sets the shared namespace.</summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the project the types currently sit in.</summary>
    public string CurrentProject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the project whose name EQUALS the namespace, or <see langword="null"/> when none exists.
    /// </summary>
    public string? ExpectedProject { get; set; }

    /// <summary>
    /// Gets or sets the closest existing ancestor project, for information only — never a move target.
    /// </summary>
    public string? NearestOwningProject { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a project named exactly <see cref="Namespace"/> exists.
    /// </summary>
    public bool ExpectedProjectExists { get; set; }

    /// <summary>
    /// Gets or sets the command that resolves this group — "MoveTypeToProject" when a project of the
    /// right name exists, otherwise the choice described by <see cref="Notice"/>.
    /// </summary>
    public string SuggestedAction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an advisory shown when no project carries this namespace, or <see langword="null"/>
    /// when the move is unambiguous.
    /// </summary>
    public string? Notice { get; set; }

    /// <summary>Gets or sets the shared mismatch kind.</summary>
    public string MismatchKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of types in this group.</summary>
    public int TypeCount { get; set; }

    /// <summary>Gets or sets the number of types in this group carrying a TypeOption attribute.</summary>
    public int TypeOptionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of types omitted from <see cref="Types"/> by the per-group cap.
    /// </summary>
    public int TypesOmitted { get; set; }

    /// <summary>
    /// Gets or sets the mismatches in this group. Empty unless the caller asked for them — a
    /// solution-wide scan carrying every type is megabytes and gets truncated before it is read.
    /// </summary>
    public IReadOnlyList<NamespaceMismatch> Types { get; set; } = Array.Empty<NamespaceMismatch>();
}
