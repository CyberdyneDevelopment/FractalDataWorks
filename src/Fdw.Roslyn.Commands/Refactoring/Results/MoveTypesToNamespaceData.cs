using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Results;

/// <summary>
/// The outcome of a <c>MoveTypesToNamespace</c> run.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MoveTypesToNamespaceData
{
    /// <summary>Gets or sets the namespace the selected types now declare.</summary>
    public string NewNamespace { get; set; } = string.Empty;

    /// <summary>Gets or sets the types that were re-homed, as old FQN -> new FQN.</summary>
    public IReadOnlyList<string> MovedTypes { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the namespaces the moved types came from.</summary>
    public IReadOnlyList<string> FromNamespaces { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the number of declarations rewritten.</summary>
    public int DeclarationsChanged { get; set; }

    /// <summary>Gets or sets the number of references followed.</summary>
    public int ReferencesFollowed { get; set; }

    /// <summary>
    /// Gets or sets the number of types left in the source namespace, untouched.
    /// </summary>
    /// <remarks>
    /// The number that makes this command different from MoveNamespace: these are the types that
    /// legitimately share the old namespace and were deliberately not moved.
    /// </remarks>
    public int TypesLeftBehind { get; set; }

    /// <summary>Gets or sets the number of moved types carrying a TypeOption, whose Id therefore changes.</summary>
    public int TypeOptionIdsChanged { get; set; }

    /// <summary>Gets or sets whether this was a preview.</summary>
    public bool WasDryRun { get; set; }

    /// <summary>Gets or sets the consumer-impact statement.</summary>
    public string ConsumerImpact { get; set; } = string.Empty;

    /// <summary>Gets or sets everything the change would break, attributed to the type causing it.</summary>
    public IReadOnlyList<BreakFinding> Breaks { get; set; } = Array.Empty<BreakFinding>();

    /// <summary>Gets or sets the number of collisions the change would cause.</summary>
    public int CollisionCount { get; set; }

    /// <summary>Gets or sets the number of references the change failed to follow.</summary>
    public int UnresolvedCount { get; set; }

    /// <summary>Gets or sets the paths of the rewritten documents.</summary>
    public IReadOnlyList<string> AffectedFiles { get; set; } = Array.Empty<string>();
}
