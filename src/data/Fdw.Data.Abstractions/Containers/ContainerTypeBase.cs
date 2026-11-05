using Fdw.Collections;
using Fdw.Conventions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for container type definitions.
/// Provides metadata about data containers (tables, views, endpoints, files).
/// </summary>
public abstract class ContainerTypeBase : TypeOptionBase<ContainerTypeBase>, IContainerType
{
    /// <summary>The category a type option states none of its own sits under.</summary>
    [PlatformDefault]
    private const string DefaultCategory = "Container";

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this container type.</param>
    /// <param name="displayName">The display name for this container type.</param>
    /// <param name="description">The description of this container type.</param>
    /// <param name="supportsSchemaDiscovery">Whether this container supports schema discovery.</param>
    /// <param name="category">The category for this container type (defaults to "Container").</param>
    protected ContainerTypeBase(
        string name,
        string displayName,
        string description,
        bool supportsSchemaDiscovery,
        string? category = null)
        : base(name, $"Containers:{name}", displayName, description, category ?? DefaultCategory)
    {
        SupportsSchemaDiscovery = supportsSchemaDiscovery;
    }

    /// <inheritdoc/>
    public bool SupportsSchemaDiscovery { get; }
}
