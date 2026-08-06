using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A named scope within a <see cref="IDataStore"/> that groups related <see cref="IDataContainer"/> instances.
/// </summary>
/// <remarks>
/// In a relational database, a path corresponds to a schema. In a file system, it is a directory.
/// In a REST API, it may represent a resource namespace.
/// <para>
/// A path is an <see cref="IDataNode"/> whose child <see cref="IDataNode.Nodes"/> are its
/// <see cref="Containers"/>. The path is the tree-navigation concept; the physical address of a
/// container (schema/table for SQL, URL for HTTP) is a separate concern carried by the container's
/// <see cref="IStorageContainer.Path"/>.
/// </para>
/// </remarks>
public interface IDataPath : IDataNode
{
    /// <summary>
    /// Gets the data store that owns this path.
    /// </summary>
    IDataStore Store { get; }

    /// <summary>
    /// Gets all containers (tables, collections, resources) within this path.
    /// </summary>
    /// <remarks>
    /// This is the typed view of <see cref="IDataNode.Nodes"/> — every element is also a child node.
    /// </remarks>
    IReadOnlyList<IDataContainer> Containers { get; }

    /// <summary>
    /// Returns the container with the given name, or a failure result if absent.
    /// </summary>
    /// <param name="name">The container name to look up.</param>
    /// <returns>
    /// Success with the matching <see cref="IDataContainer"/>, or Failure when no container with
    /// <paramref name="name"/> exists. Callers MUST check <c>IsSuccess</c> before using <c>.Value</c>.
    /// </returns>
    IGenericResult<IDataContainer> Container(string name);
}
