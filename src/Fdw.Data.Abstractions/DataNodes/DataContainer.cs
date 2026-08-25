using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Concrete base implementation of <see cref="IDataContainer"/> — a container that is simultaneously
/// a uniform <see cref="IDataNode"/> (its child <see cref="Nodes"/> are its <see cref="IDataField"/>
/// fields) and an <see cref="IStorageContainer"/> (its <see cref="Schema"/> is a synchronous
/// projection over those field children).
/// </summary>
/// <remarks>
/// Why: replaces the old async-fields <c>DataContainer</c> plus the <c>ContainerBase</c> sync-schema
/// family and the <c>MaterializedDataContainer</c> bridge. A built container is complete: fields are
/// set at construction, <see cref="Schema"/> projects them on demand, <see cref="Keys"/> and
/// <see cref="ReferencingKeys"/> are present. There is no materialization step and no sync-over-async.
/// Transport subclasses (<c>MsSqlTableContainer</c>, <c>PostgreSqlTableContainer</c>) inherit this and
/// override only the storage-specific members; the generic base serves Http/file transports.
/// <para>
/// Why this lives in Data.Abstractions: every transport package (<c>Data.MsSql</c>,
/// <c>Data.PostgreSql</c>, generic Http/file) inherits this base, and those packages are upstream of
/// <c>Services.Data</c>. The base therefore MUST sit upstream of all of them — in Data.Abstractions,
/// alongside <see cref="IDataContainer"/>/<see cref="ContainerSchema"/> — so a transport subclass never
/// needs a (cycle-forming) reference back into <c>Services.Data</c>.
/// </para>
/// </remarks>
public class DataContainer : IDataContainer
{
    private readonly ILogger _logger;
    private readonly IReadOnlyList<IDataField> _fields;
    private readonly IReadOnlyList<IContainerKey> _keys;
    private readonly IGenericResult<IReadOnlyList<ReferencingKeyBinding>> _referencingKeys;
    private readonly IContainerType _containerType;
    private readonly IFormatType _format;
    private readonly IPath _physicalPath;
    private readonly string[] _supportedOperations;
    private readonly IReadOnlyDictionary<string, object> _metadata;
    private readonly Dictionary<string, IDataField> _fieldIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainer"/> class with its fields, keys,
    /// physical address, format, and metadata fully resolved at construction.
    /// </summary>
    /// <param name="name">The container name.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="parent">The tree-navigation parent path that owns this container.</param>
    /// <param name="fields">The field child nodes (also projected to <see cref="Schema"/>). Empty when the container carries no field schema (for example a generic HTTP container).</param>
    /// <param name="keys">The keys defined on this container.</param>
    /// <param name="referencingKeys">The inbound FK references to this container.</param>
    /// <param name="containerType">The storage mechanism (Table, View, Endpoint, File).</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="physicalPath">The physical address (transport location) read by translators.</param>
    /// <param name="supportedOperations">The operations the container supports.</param>
    /// <param name="metadata">Container metadata.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public DataContainer(
        string name,
        string? description,
        IDataNodePath parent,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys,
        IContainerType containerType,
        IFormatType format,
        IPath physicalPath,
        string[] supportedOperations,
        IReadOnlyDictionary<string, object> metadata,
        ILogger? logger = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _referencingKeys = referencingKeys ?? throw new ArgumentNullException(nameof(referencingKeys));
        _containerType = containerType ?? throw new ArgumentNullException(nameof(containerType));
        _format = format ?? throw new ArgumentNullException(nameof(format));
        _physicalPath = physicalPath ?? throw new ArgumentNullException(nameof(physicalPath));
        _supportedOperations = supportedOperations ?? throw new ArgumentNullException(nameof(supportedOperations));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        // Why: NullLogger keeps the container functional when DI logging is not wired — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;

        // Why: O(1) lookup dictionary — field names are unique within a container.
        _fieldIndex = new Dictionary<string, IDataField>(StringComparer.Ordinal);
        for (var i = 0; i < fields.Count; i++)
        {
            _fieldIndex[fields[i].Name] = fields[i];
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContainer"/> class as a DETACHED container —
    /// one reached by direct physical address (a one-off SQL query container, for example) rather than
    /// by DataStore→Path→Container tree navigation. A structurally-valid empty <see cref="DetachedDataPath"/>
    /// (over a <see cref="DetachedDataStore"/> named after the schema) is supplied as its tree parent.
    /// </summary>
    /// <param name="name">The container name.</param>
    /// <param name="schemaName">The schema/path name the detached parent path is named after.</param>
    /// <param name="fields">The field child nodes (also projected to <see cref="Schema"/>). Empty when the container carries no field schema.</param>
    /// <param name="keys">The keys defined on this container.</param>
    /// <param name="referencingKeys">The inbound FK references to this container.</param>
    /// <param name="containerType">The storage mechanism (Table, View, Endpoint, File).</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="physicalPath">The physical address (transport location) read by translators.</param>
    /// <param name="supportedOperations">The operations the container supports.</param>
    /// <param name="metadata">Container metadata.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public DataContainer(
        string name,
        string schemaName,
        IReadOnlyList<IDataField> fields,
        IReadOnlyList<IContainerKey> keys,
        IGenericResult<IReadOnlyList<ReferencingKeyBinding>> referencingKeys,
        IContainerType containerType,
        IFormatType format,
        IPath physicalPath,
        string[] supportedOperations,
        IReadOnlyDictionary<string, object> metadata,
        ILogger? logger = null)
        : this(
            name,
            null,
            new DetachedDataPath(
                schemaName ?? throw new ArgumentNullException(nameof(schemaName)),
                new DetachedDataStore(schemaName, logger),
                logger),
            fields,
            keys,
            referencingKeys,
            containerType,
            format,
            physicalPath,
            supportedOperations,
            metadata,
            logger)
    {
    }

    /// <inheritdoc cref="IDataContainer.Name" />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataNodePath Parent { get; }

    /// <inheritdoc />
    // Why: a container's children ARE its fields — the uniform IDataNode child surface over the
    // same field set that Schema projects. IReadOnlyList<IDataField> is covariant to IReadOnlyList<IDataNode>.
    public IReadOnlyList<IDataNode> Nodes => _fields;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
    {
        if (_fieldIndex.TryGetValue(name, out var field))
            return GenericResult<IDataNode>.Success(field);

        return GenericResult<IDataNode>.Failure(
            DataNodeTreeLog.FieldNotFoundInContainer(_logger, name, Name));
    }

    /// <inheritdoc />
    public IReadOnlyList<IContainerKey> Keys => _keys;

    /// <inheritdoc />
    public IGenericResult<IReadOnlyList<ReferencingKeyBinding>> ReferencingKeys => _referencingKeys;

    /// <summary>Gets the field child nodes of this container.</summary>
    protected IReadOnlyList<IDataField> Fields => _fields;

    // -------------------------------------------------------------------------
    // IStorageContainer — the container IS a storage container; no materialization.
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public virtual IContainerType ContainerType => _containerType;

    /// <inheritdoc />
    public virtual IFormatType Format => _format;

    /// <inheritdoc />
    // Why: Schema is a SYNCHRONOUS projection over the field children — the runtime field types
    // implement IField, so the schema fields are the field children cast to IField. A field that is
    // not an IField is a contract violation (fail loud), not a runtime data condition. When the
    // container carries no field schema (generic HTTP), the projection is empty.
    public virtual IContainerSchema Schema => ProjectSchema();

    /// <inheritdoc />
    public virtual IPath Path => _physicalPath;

    /// <inheritdoc />
    public virtual string[] SupportedOperations => _supportedOperations;

    /// <inheritdoc />
    public virtual IReadOnlyDictionary<string, object> Metadata => _metadata;

    // Why: concrete return type (not IContainerSchema) per CA1859 — Data.Abstractions treats it as an error.
    private ContainerSchema ProjectSchema()
    {
        var projected = new IField[_fields.Count];
        for (var i = 0; i < _fields.Count; i++)
        {
            // Why: every runtime field implements IField; a field that does not is a contract
            // violation — throw rather than silently skip (no fallback).
            projected[i] = _fields[i] as IField
                ?? throw new InvalidOperationException(
                    $"Field '{_fields[i].Name}' on container '{Name}' does not implement IField.");
        }

        return new ContainerSchema
        {
            Fields = projected,
            Name = Name,
        };
    }
}
