using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// A PostgreSQL table container: the concrete <see cref="DataContainer"/> base specialised for the
/// PostgreSQL transport. Its physical address is a <see cref="PostgreSqlDatabasePath"/>; its container
/// type is <c>PostgreSqlTable</c>; its <see cref="DataContainer.Schema"/> projects the
/// <see cref="PostgreSqlDataField"/> child nodes (which implement <see cref="IField"/>).
/// </summary>
/// <remarks>
/// Why: replaces the old async-fields <c>PostgreSqlDataContainer</c> and its narrowing
/// <c>IPostgreSqlDataContainer</c> interface. A built container is complete — fields, keys, and
/// referencing keys are supplied at construction and there is no materialization step. The
/// PostgreSQL translators read the physical location from <see cref="DataContainer.Path"/>
/// (a <see cref="PostgreSqlDatabasePath"/>) and the field list from <see cref="DataContainer.Schema"/>.
/// </remarks>
public sealed class PostgreSqlTableContainer : DataContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTableContainer"/> class.
    /// </summary>
    /// <param name="name">The table name.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="parent">The tree-navigation parent path that owns this container.</param>
    /// <param name="fields">The PostgreSQL field child nodes (also projected to the schema).</param>
    /// <param name="keys">The keys defined on this container.</param>
    /// <param name="referencingKeys">The inbound FK references to this container.</param>
    /// <param name="physicalPath">The physical PostgreSQL address read by the translators.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="metadata">Container metadata.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public PostgreSqlTableContainer(
        string name,
        string? description,
        IDataPath parent,
        IReadOnlyList<IPostgreSqlDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys,
        PostgreSqlDatabasePath physicalPath,
        IFormatType format,
        IReadOnlyDictionary<string, object> metadata,
        ILogger<PostgreSqlTableContainer>? logger = null)
        : base(
            name,
            description,
            parent,
            fields,
            keys,
            referencingKeys,
            // Why: ContainerTypes is a TypeCollection populated by module initializers in entry-point
            // apps; ByName resolves the PostgreSql table option registered by PostgreSqlTableContainerType.
            ContainerTypes.ByName("PostgreSqlTable"),
            format,
            physicalPath,
            // Why: real table operations — drives gateway operation validation.
            ["Query", "Insert", "Update", "Delete"],
            metadata,
            logger)
    {
    }
}
