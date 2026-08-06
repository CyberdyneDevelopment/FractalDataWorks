using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// The outcome of a <c>MoveNamespace</c> run.
/// </summary>
// Why: pure data holder, auto-properties only, no logic
[ExcludeFromCodeCoverage]
public sealed class MoveNamespaceData
{
    /// <summary>Gets or sets the namespace before the rename.</summary>
    public string OldNamespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the namespace after the rename.</summary>
    public string NewNamespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of documents rewritten.</summary>
    public int DocumentsChanged { get; set; }

    /// <summary>Gets or sets the number of individual references rewritten.</summary>
    public int ReferencesRewritten { get; set; }

    /// <summary>Gets or sets the number of types whose declaring namespace changed.</summary>
    public int TypesRenamed { get; set; }

    /// <summary>
    /// Gets or sets the number of affected types carrying a TypeOption attribute, whose FNV-1a
    /// <c>Id</c> therefore changes.
    /// </summary>
    public int TypeOptionIdsChanged { get; set; }

    /// <summary>Gets or sets whether this was a preview.</summary>
    public bool WasDryRun { get; set; }

    /// <summary>
    /// Gets or sets the consumer-impact statement for this run.
    /// </summary>
    public string ConsumerImpact { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets everything the rewrite would leave broken — collisions, and references it failed to
    /// follow.
    /// </summary>
    /// <remarks>
    /// A namespace rewrite is the most far-reaching change here, so the preview has to say what it breaks.
    /// An UnresolvedReference finding is the actionable one: something still points at the old name and
    /// needs following.
    /// </remarks>
    public IReadOnlyList<BreakFinding> Breaks { get; set; } = Array.Empty<BreakFinding>();

    /// <summary>Gets or sets the number of collisions the rewrite would cause.</summary>
    public int CollisionCount { get; set; }

    /// <summary>Gets or sets the number of references the rewrite failed to follow.</summary>
    public int UnresolvedCount { get; set; }

    /// <summary>Gets or sets the paths of the rewritten documents.</summary>
    public IReadOnlyList<string> AffectedFiles { get; set; } = Array.Empty<string>();
}
