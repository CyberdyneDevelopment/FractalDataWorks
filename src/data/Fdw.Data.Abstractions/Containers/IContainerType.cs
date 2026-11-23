using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a container type definition - metadata about data containers.
/// </summary>
/// <remarks>
/// Container types describe data sources like tables, views, API endpoints, files.
/// </remarks>
public interface IContainerType : ITypeOption<int>
{
    /// <summary>
    /// Gets the configuration key for this container type value.
    /// </summary>
    string ConfigurationKey { get; }

    /// <summary>
    /// Gets the display name for this container type value.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of this container type value.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets whether this container type supports schema discovery.
    /// </summary>
    bool SupportsSchemaDiscovery { get; }

    /// <summary>
    /// Gets the operations a container of this type supports: <c>Query</c>, <c>Insert</c>, <c>Update</c>, <c>Delete</c>. The type states
    /// them; no code decides them from the type's name (FDW-6309).
    /// </summary>
    IReadOnlyList<string> SupportedOperations { get; }
}
