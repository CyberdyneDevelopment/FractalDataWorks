namespace Fdw.Roslyn.Commands.Analysis.Results;

/// <summary>
/// Represents a call hierarchy entry.
/// </summary>
public sealed class CallHierarchyEntry
{
    /// <summary>
    /// Gets or sets the caller/callee name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the caller/callee full name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets or sets the containing type.
    /// </summary>
    public required string ContainingType { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets or sets the depth in the hierarchy.
    /// </summary>
    public required int Depth { get; init; }
}