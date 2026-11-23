using System.Collections.Generic;
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
    /// <param name="supportedOperations">
    /// The operations a container of this type supports (<c>Query</c>, <c>Insert</c>, <c>Update</c>, <c>Delete</c>), stated by the option
    /// and never chosen by the code that builds a container. Required, with no default: the NotFound placeholder is built with an empty
    /// list, so this constructor does not refuse one, and a test over <c>ContainerTypes.All()</c> checks that every real option states at
    /// least one (FDW-6309).
    /// </param>
    /// <param name="category">The category for this container type (defaults to "Container").</param>
    protected ContainerTypeBase(
        string name,
        string displayName,
        string description,
        bool supportsSchemaDiscovery,
        IReadOnlyList<string> supportedOperations,
        string? category = null)
        : base(name, $"Containers:{name}", displayName, description, category ?? DefaultCategory)
    {
        SupportsSchemaDiscovery = supportsSchemaDiscovery;
        SupportedOperations = supportedOperations;
    }

    /// <inheritdoc/>
    public bool SupportsSchemaDiscovery { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedOperations { get; }
}
