using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for path type definitions.
/// Provides metadata about navigation paths to data containers.
/// </summary>
public abstract class PathTypeBase : TypeOptionBase<int, PathTypeBase>, IPathType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this path type.</param>
    /// <param name="name">The name of this path type.</param>
    /// <param name="displayName">The display name for this path type.</param>
    /// <param name="description">The description of this path type.</param>
    /// <param name="domain">The domain this path belongs to (Sql, Rest, File, GraphQL).</param>
    /// <param name="category">The category for this path type (defaults to "Path").</param>
    protected PathTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        string domain,
        string? category = null)
        : base(id, name, $"Paths:{name}", displayName, description, category ?? "Path")
    {
        Domain = domain;
    }

    /// <inheritdoc/>
    public string Domain { get; }
}
