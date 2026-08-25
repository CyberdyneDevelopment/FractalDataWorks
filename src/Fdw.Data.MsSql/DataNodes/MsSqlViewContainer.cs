using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.MsSql;

/// <summary>
/// A SQL Server view container: the concrete <see cref="DataContainer"/> base specialised for the
/// MsSql transport with read-only operations. Its physical address is a <see cref="DatabasePath"/>;
/// its container type is <c>View</c>; its <see cref="DataContainer.Schema"/> projects the
/// <see cref="IMsSqlDataField"/> child nodes (which implement <see cref="IField"/>).
/// </summary>
/// <remarks>
/// Why: replaces the old <c>ContainerBase</c>-derived <c>ViewContainer</c>. A built container is
/// complete — fields, keys, and referencing keys are supplied at construction and there is no
/// materialization step. Views are read-only, so the only supported operation is Query.
/// </remarks>
public sealed class MsSqlViewContainer : DataContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlViewContainer"/> class.
    /// </summary>
    /// <param name="name">The view name.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="parent">The tree-navigation parent path that owns this container.</param>
    /// <param name="fields">The SQL Server field child nodes (also projected to the schema).</param>
    /// <param name="keys">The keys defined on this container.</param>
    /// <param name="referencingKeys">The inbound FK references to this container.</param>
    /// <param name="physicalPath">The physical SQL Server address read by the translators.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="metadata">Container metadata.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public MsSqlViewContainer(
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
            // Why: instantiate the View container type directly. ViewContainerType is registered
            // RestrictToCurrentCompilation=true (Data.MsSql-local), so a shared-registry ByName lookup
            // is not reliable across assemblies; the concrete type is the source of truth here.
            new ViewContainerType(),
            format,
            physicalPath,
            // Why: views are read-only — only Query is a valid operation.
            ["Query"],
            metadata,
            logger)
    {
    }
}
