using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a data container that defines the physical format and structure of data.
/// </summary>
/// <remarks>
/// A data container combines:
/// - ContainerType: The storage mechanism (Table, View, Endpoint, File)
/// - Format: The serialization format (Tabular, Json, Xml, Csv)
/// - Schema: The field/column definitions
/// - Path: The location within a data store
///
/// Note: Named IStorageContainer (not IContainer) to avoid confusion with DI/IoC containers.
/// </remarks>
public interface IStorageContainer
{
    /// <summary>
    /// Gets the name of this container.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the container type (storage mechanism).
    /// </summary>
    /// <remarks>
    /// Examples: Table, View, StoredProcedure, Endpoint, File, Queue
    /// </remarks>
    IContainerType ContainerType { get; }

    /// <summary>
    /// Gets the data format (serialization format).
    /// </summary>
    /// <remarks>
    /// Examples: Tabular, Json, Xml, Csv, Parquet, Protobuf
    /// </remarks>
    IFormatType Format { get; }

    /// <summary>
    /// Gets the container schema (fields/columns).
    /// </summary>
    /// <remarks>
    /// Why: for an <see cref="IDataContainer"/> this is a synchronous projection over the container's
    /// <see cref="IDataField"/> child nodes — there is no async fetch and no materialization step.
    /// The runtime field types implement <see cref="IField"/>, so the schema is the field children
    /// cast to <see cref="IField"/>.
    /// </remarks>
    IContainerSchema Schema { get; }

    /// <summary>
    /// Gets the physical address of this container within its store (the transport location).
    /// </summary>
    /// <remarks>
    /// For SQL this is a <c>DatabasePath</c> (Database.Schema.Object) that the translators read via
    /// <c>container.Path is DatabasePath</c>; for HTTP a <c>HttpPath</c>; for files a <c>FilePath</c>.
    /// This is distinct from <see cref="IDataContainer.Parent"/>, which is the tree-navigation path node.
    /// </remarks>
    IPath Path { get; }

    /// <summary>
    /// Gets the supported operations for this container.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    /// <item>SQL Table: ["Query", "Insert", "Update", "Delete"] - full CRUD</item>
    /// <item>SQL View: ["Query"] - read-only</item>
    /// <item>REST Endpoint: ["Query", "Insert"] - limited operations</item>
    /// </list>
    /// Used by DataGateway for validation and routing decisions.
    /// </remarks>
    string[] SupportedOperations { get; }

    /// <summary>
    /// Gets metadata about this container.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
