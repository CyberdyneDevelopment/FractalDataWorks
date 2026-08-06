using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for container type definitions.
/// Provides metadata about data containers (tables, views, endpoints, files).
/// </summary>
public abstract class ContainerTypeBase : TypeOptionBase<int, ContainerTypeBase>, IContainerType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this container type.</param>
    /// <param name="name">The name of this container type.</param>
    /// <param name="displayName">The display name for this container type.</param>
    /// <param name="description">The description of this container type.</param>
    /// <param name="supportsSchemaDiscovery">Whether this container supports schema discovery.</param>
    /// <param name="category">The category for this container type (defaults to "Container").</param>
    protected ContainerTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        bool supportsSchemaDiscovery,
        string? category = null)
        : base(id, name, $"Containers:{name}", displayName, description, category ?? "Container")
    {
        SupportsSchemaDiscovery = supportsSchemaDiscovery;
    }

    /// <inheritdoc/>
    public bool SupportsSchemaDiscovery { get; }
}
