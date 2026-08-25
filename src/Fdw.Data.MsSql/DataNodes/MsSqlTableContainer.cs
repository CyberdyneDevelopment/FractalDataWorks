using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql;

/// <summary>
/// A SQL Server table container: the concrete <see cref="DataContainer"/> base specialised for the
/// MsSql transport. Its physical address is a <see cref="DatabasePath"/>; its container type is
/// <c>Table</c>; its <see cref="DataContainer.Schema"/> projects the <see cref="IMsSqlDataField"/>
/// child nodes (which implement <see cref="IField"/>).
/// </summary>
/// <remarks>
/// Why: replaces the old async-fields <c>MsSqlDataContainer</c> and the <c>ContainerBase</c>-derived
/// <c>TableContainer</c>. A built container is complete — fields, keys, and referencing keys are
/// supplied at construction and there is no materialization step. The MsSql translators read the
/// physical location from <see cref="DataContainer.Path"/> (a <see cref="DatabasePath"/>) and the
/// field list from <see cref="DataContainer.Schema"/> (the projection over the typed field children).
/// </remarks>
public sealed class MsSqlTableContainer : DataContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlTableContainer"/> class.
    /// </summary>
    /// <param name="name">The table name.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="parent">The tree-navigation parent path that owns this container.</param>
    /// <param name="fields">The SQL Server field child nodes (also projected to the schema).</param>
    /// <param name="keys">The keys defined on this container.</param>
    /// <param name="referencingKeys">The inbound FK references to this container.</param>
    /// <param name="physicalPath">The physical SQL Server address read by the translators.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="metadata">Container metadata.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public MsSqlTableContainer(
        string name,
        string? description,
        IDataNodePath parent,
        IReadOnlyList<IMsSqlDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys,
        DatabasePath physicalPath,
        IFormatType format,
        IReadOnlyDictionary<string, object> metadata,
        ILogger? logger = null)
        : base(
            name,
            description,
            parent,
            fields,
            keys,
            referencingKeys,
            // Why: instantiate the Table container type directly. TableContainerType is registered
            // RestrictToCurrentCompilation=true (Data.MsSql-local), so a shared-registry ByName lookup
            // is not reliable across assemblies; the concrete type is the source of truth here.
            new TableContainerType(),
            format,
            physicalPath,
            // Why: real table operations — drives gateway operation validation.
            ["Query", "Insert", "Update", "Delete"],
            metadata,
            logger)
    {
    }
}
