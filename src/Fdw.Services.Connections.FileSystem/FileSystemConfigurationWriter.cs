using Fdw.Data.FileSystem;
using Fdw.Data.FileSystem.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Services.Connections.FileSystem.Logging;
using Fdw.Services.Connections.RowQuery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// Runs the FileSystem configuration write verbs — the version-on-write CREATE (<c>ConfigurationSave</c>),
/// the literal in-place <c>Update</c>, and the soft-delete (<c>ConfigurationDelete</c>) — by reading the
/// container's current rows through the <see cref="FileSystemRecordConnector"/>, mutating the in-memory
/// row list per the verb's semantics, and rewriting the WHOLE file. This mirrors the ACTUAL MsSql
/// translator semantics (<c>MsSqlConfigurationSaveTranslator</c>, <c>MsSqlUpdateTranslator</c>,
/// <c>MsSqlConfigurationDeleteTranslator</c>) over a flat name→value row set instead of T-SQL.
/// </summary>
/// <remarks>
/// Why a dedicated writer (not inlined in the connection): the read-modify-write logic is substantial
/// (POCO mapping, container-key-driven logical/physical/FK resolution, version-flag management) and needs
/// both the container metadata AND the record connector (for parent-file reads on FK resolution). Keeping
/// it here keeps the connection's <c>Execute</c> a thin dispatcher and the metadata derivation cohesive.
/// The key/FK resolution is driven off <see cref="IDataContainer.Keys"/> — never a hardcoded column name
/// or a per-container-name special case — exactly as the MsSql translators derive theirs.
/// <para>
/// Why the per-file lock: every verb is read-whole-file → mutate in memory → write-whole-file with no
/// coordination between concurrent callers. Two near-simultaneous writes to the SAME file (e.g. <see cref="NextRowId"/>
/// computing <c>max(existing RowId)+1</c> from an in-memory snapshot) can both read the same current-max
/// RowId and both compute the same "next" RowId — the second write silently clobbers the first: one row
/// vanishes, a duplicate RowId remains, and BOTH calls report <c>Success</c>. <see cref="_fileLocks"/>
/// serializes the full read-modify-write cycle per resolved absolute file path so concurrent writers
/// FROM THIS PROCESS never race. See <see cref="_fileLocks"/> for the explicit boundary this does NOT cover.
/// </para>
/// </remarks>
internal sealed class FileSystemConfigurationWriter
{
    // Why: the version-on-write audit columns are a framework-wide convention (every config container
    // declares them), the same literals MsSql's translators hardcode in their SET/VALUES clauses. They
    // are managed by the translator, never read from the POCO.
    private const string IsCurrentColumn = "IsCurrent";
    private const string IsDeletedColumn = "IsDeleted";

    // Why: same case-sensitivity convention PathCanonicalizer uses (Linux is case-sensitive, Windows is
    // not) — two relative paths that resolve to the SAME physical file on this OS must share ONE lock.
    private static readonly StringComparer _lockKeyComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// Per-absolute-file-path locks serializing the read-modify-write cycle of <see cref="Save"/>/
    /// <see cref="Update"/>/<see cref="Delete"/> against the SAME file.
    /// </summary>
    /// <remarks>
    /// Explicit boundary: this serializes writes to the same file FROM THIS PROCESS only. A second,
    /// different OS process writing the same file at the same instant is NOT guarded here — that would
    /// need real OS-level file locking or optimistic concurrency (e.g. an ETag/version check), and is out
    /// of scope for this pass: the configuration domain is low-write-frequency and has no current
    /// multi-process writer (a single app process owns the ConfigurationGateway connection).
    /// </remarks>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new(_lockKeyComparer);

    private readonly FileSystemRecordConnector _connector;
    private readonly string _connectionName;
    private readonly string _root;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemConfigurationWriter"/> class.
    /// </summary>
    /// <param name="connector">The record connector that performs the config-driven file read/write.</param>
    /// <param name="connectionName">The owning connection name, for structured logging.</param>
    /// <param name="root">The connection's canonicalized Root — used to resolve the absolute file path each verb locks on.</param>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public FileSystemConfigurationWriter(FileSystemRecordConnector connector, string connectionName, string root, ILogger? logger)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _connectionName = connectionName;
        _root = root;
        // Why: NullLogger keeps the writer functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
    }

    // Why: resolves the SAME absolute path the actual I/O will use (PathCanonicalizer.Resolve is pure
    // string manipulation, no I/O) — the lock key must match the physical file the verb is about to touch.
    private IGenericResult<string> ResolveAbsolutePath(string relativePath)
        => PathCanonicalizer.Resolve(_root, relativePath, _connectionName, _logger);

    private static SemaphoreSlim GetFileLock(string absolutePath) =>
        _fileLocks.GetOrAdd(absolutePath, static _ => new SemaphoreSlim(1, 1));

    // ════════════════════════════════════════════════════════════════════════════
    // ConfigurationSave — version-on-write CREATE
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Persists a new logical version of a configuration record: assigns a new physical RowId, resolves
    /// FK RowId columns against the parent file, sets <c>IsCurrent=true</c>/<c>IsDeleted=false</c>, retires
    /// any prior current version of the same logical key, appends the new row, and rewrites the whole file.
    /// </summary>
    public async Task<IGenericResult<int>> Save(
        FileSystemConfigurationSaveCommand command, CancellationToken cancellationToken = default)
    {
        var pathResult = ResolveAbsolutePath(command.RelativePath);
        if (!pathResult.IsSuccess)
            return pathResult.ToNewResult<int>();

        var fileLock = GetFileLock(pathResult.Value!);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await SaveCore(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<IGenericResult<int>> SaveCore(
        FileSystemConfigurationSaveCommand command, CancellationToken cancellationToken)
    {
        var container = command.Container;
        FileSystemConfigurationWriteLog.SaveStarting(_logger, _connectionName, container.Name);

        var logicalKeyColumn = LogicalKeyColumn(container);
        if (logicalKeyColumn is null)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.LogicalKeyNotFound(_logger, _connectionName, container.Name));

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        var rows = rowsResult.Value!.Select(CopyRow).ToList();

        var newRowResult = await BuildNewRow(command, rows, cancellationToken).ConfigureAwait(false);
        if (!newRowResult.IsSuccess)
            return newRowResult.ToNewResult<int>();
        var newRow = newRowResult.Value!;

        // Why: guarantee the assembled row satisfies the container's declared schema BEFORE it is written —
        // a declared non-nullable column left null is a fail-loud defect, caught once by the shared validator.
        var validate = RecordRowValidator.Validate(new[] { (IReadOnlyDictionary<string, object?>)newRow }, container, _logger);
        if (!validate.IsSuccess)
            return validate.ToNewResult<int>();

        RetireCurrentVersions(rows, container, logicalKeyColumn, GetCell(newRow, logicalKeyColumn));
        rows.Add(newRow);

        var writeResult = await WriteRows(container, command.RelativePath, rows, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
            return writeResult;

        FileSystemConfigurationWriteLog.SaveCompleted(_logger, _connectionName, container.Name, 1);
        return GenericResult<int>.Success(1);
    }

    private async Task<IGenericResult<Dictionary<string, object?>>> BuildNewRow(
        FileSystemConfigurationSaveCommand command,
        IReadOnlyList<Dictionary<string, object?>> currentRows,
        CancellationToken cancellationToken)
    {
        var container = command.Container;

        var mapper = PocoMapperCollection.ByName(command.Record.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<Dictionary<string, object?>>.Failure(
                FileSystemConfigurationWriteLog.PocoMapperNotFound(_logger, _connectionName, command.Record.GetType().Name));

        var physicalKeyColumn = PhysicalKeyColumn(container);
        if (physicalKeyColumn is null)
            return GenericResult<Dictionary<string, object?>>.Failure(
                FileSystemConfigurationWriteLog.PhysicalKeyNotFound(_logger, _connectionName, container.Name));

        var parameters = new Dictionary<string, object?>(mapper.MapToParameters(command.Record), StringComparer.OrdinalIgnoreCase);
        var propertyNames = new HashSet<string>(mapper.GetPropertyNames(), StringComparer.OrdinalIgnoreCase);
        MergeAdditionalColumns(command.AdditionalColumnValues, parameters, propertyNames);

        var insertable = InsertableFieldNames(container);
        var newRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        AddMappedColumns(newRow, propertyNames, parameters, insertable, physicalKeyColumn);

        var fkResult = await ResolveForeignKeys(container, parameters, propertyNames, insertable, cancellationToken).ConfigureAwait(false);
        if (!fkResult.IsSuccess)
            return fkResult.ToNewResult<Dictionary<string, object?>>();
        foreach (var fk in fkResult.Value!)
            newRow[fk.Key] = fk.Value;

        if (newRow.Count == 0)
            return GenericResult<Dictionary<string, object?>>.Failure(
                FileSystemConfigurationWriteLog.NoInsertableColumns(
                    _logger, _connectionName, container.Name, command.Record.GetType().Name));

        // Why: RowId is the physical version PK (INT IDENTITY in MsSql — DB-assigned there). No DB assigns
        // it here, so the translator explicitly stamps the next value; IsCurrent/IsDeleted are set by the
        // translator, never read from the POCO (mirrors MsSql's hardcoded VALUES (..., 1, 0)).
        newRow[physicalKeyColumn] = NextRowId(currentRows, physicalKeyColumn);
        newRow[IsCurrentColumn] = true;
        newRow[IsDeletedColumn] = false;

        return GenericResult<Dictionary<string, object?>>.Success(newRow);
    }

    // Why: mirrors MsSqlConfigurationSaveTranslator's ResolveForeignKeys — a FK RowId column exists in the
    // child container but not on the POCO (the POCO carries only the logical Id, e.g. SecretManagerId). The
    // physical RowId is resolved at write time against the parent's current row, exactly as MsSql subqueries
    // SELECT RowId FROM parent WHERE Id = @LogicalId AND IsCurrent = 1.
    private async Task<IGenericResult<Dictionary<string, object?>>> ResolveForeignKeys(
        IDataContainer container,
        Dictionary<string, object?> parameters,
        HashSet<string> propertyNames,
        HashSet<string> insertable,
        CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var keys = container.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (!string.Equals(key.KeyType.Name, "Foreign", StringComparison.Ordinal) || key.KeyFields.Count == 0)
                continue;
            var referenced = key.ReferencedContainer;
            if (referenced is null)
                continue;

            var fkColumn = key.KeyFields[0].LocalField.Name;
            // Only resolve FK columns the container declares but the POCO does not supply directly.
            if (propertyNames.Contains(fkColumn) || !insertable.Contains(fkColumn))
                continue;

            var fkValueResult = await ResolveParentRowId(container, referenced, fkColumn, parameters, cancellationToken).ConfigureAwait(false);
            if (!fkValueResult.IsSuccess)
                return fkValueResult.ToNewResult<Dictionary<string, object?>>();
            resolved[fkColumn] = fkValueResult.Value;
        }

        return GenericResult<Dictionary<string, object?>>.Success(resolved);
    }

    private async Task<IGenericResult<long>> ResolveParentRowId(
        IDataContainer container,
        IDataContainer referenced,
        string fkColumn,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        // Why: derive the child's logical FK value from the FK column name by the same convention MsSql uses
        // ({Parent}RowId → {Parent}Id), then match it against the parent's own logical key.
        var logicalColumn = fkColumn.EndsWith("RowId", StringComparison.Ordinal)
            ? string.Concat(fkColumn.AsSpan(0, fkColumn.Length - "RowId".Length), "Id")
            : fkColumn;

        if (!parameters.TryGetValue(logicalColumn, out var logicalValue) || logicalValue is null)
            return GenericResult<long>.Failure(
                FileSystemConfigurationWriteLog.ForeignKeyLogicalValueMissing(
                    _logger, _connectionName, container.Name, fkColumn, logicalColumn));

        // Why: key.ReferencedContainer is a lighter FK-target node whose Keys/Path are not fully populated;
        // navigate to the SAME-path sibling in the built tree (the proven LoadJoinedRows pattern) to get the
        // fully-built parent node with its keys, physical path, and current rows.
        var parentResult = container.Parent.Container(referenced.Name);
        if (!parentResult.IsSuccess || parentResult.Value is null)
            return GenericResult<long>.Failure(
                FileSystemConfigurationWriteLog.ForeignKeyParentContainerNotResolved(
                    _logger, _connectionName, container.Name, referenced.Name));
        var parent = parentResult.Value;

        var parentLogicalKey = LogicalKeyColumn(parent);
        var parentPhysicalKey = PhysicalKeyColumn(parent);
        if (parentLogicalKey is null || parentPhysicalKey is null)
            return GenericResult<long>.Failure(
                FileSystemConfigurationWriteLog.LogicalKeyNotFound(_logger, _connectionName, parent.Name));

        var parentRowsResult = await _connector
            .ReadExistingOrEmpty(parent, parent.Path.PathValue, cancellationToken).ConfigureAwait(false);
        if (!parentRowsResult.IsSuccess)
            return parentRowsResult.ToNewResult<long>();

        foreach (var parentRow in parentRowsResult.Value!)
        {
            if (!KeyValuesEqual(GetCell(parentRow, parentLogicalKey), logicalValue) || !IsFlagTrue(GetCell(parentRow, IsCurrentColumn)))
                continue;
            var rowId = ToLong(GetCell(parentRow, parentPhysicalKey));
            if (rowId is not null)
                return GenericResult<long>.Success(rowId.Value);
        }

        // NO FALLBACKS: never write a dangling/zero FK — fail loud when no current parent row matches.
        return GenericResult<long>.Failure(
            FileSystemConfigurationWriteLog.ForeignKeyParentNotFound(
                _logger, _connectionName, container.Name, fkColumn, parent.Name,
                Convert.ToString(logicalValue, CultureInfo.InvariantCulture) ?? string.Empty));
    }

    private void RetireCurrentVersions(
        IReadOnlyList<Dictionary<string, object?>> rows,
        IDataContainer container,
        string logicalKeyColumn,
        object? newLogicalValue)
    {
        var retired = false;
        foreach (var row in rows)
        {
            if (!KeyValuesEqual(GetCell(row, logicalKeyColumn), newLogicalValue) || !IsFlagTrue(GetCell(row, IsCurrentColumn)))
                continue;
            row[IsCurrentColumn] = false;
            retired = true;
        }

        if (retired)
            FileSystemConfigurationWriteLog.PriorVersionRetired(_logger, _connectionName, container.Name);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Update — literal, in-place mutation (NOT version-on-write)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mutates the matched rows' non-key columns in place (same RowId, version flags untouched) and
    /// rewrites the whole file. Matches on the command's filter; refuses to run without one.
    /// </summary>
    public async Task<IGenericResult<int>> Update(
        FileSystemUpdateCommand command, CancellationToken cancellationToken = default)
    {
        var pathResult = ResolveAbsolutePath(command.RelativePath);
        if (!pathResult.IsSuccess)
            return pathResult.ToNewResult<int>();

        var fileLock = GetFileLock(pathResult.Value!);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await UpdateCore(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<IGenericResult<int>> UpdateCore(
        FileSystemUpdateCommand command, CancellationToken cancellationToken)
    {
        var container = command.Container;
        FileSystemConfigurationWriteLog.UpdateStarting(_logger, _connectionName, container.Name);

        // Why: the config Update always carries a WHERE (Id = record.Id). No filter means no way to identify
        // rows — refuse rather than silently mutate every row (NO FALLBACKS), unlike a bare SQL UPDATE.
        if (command.Filter?.Root is null)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.UpdateFilterMissing(_logger, _connectionName, container.Name));

        var filterColumns = RecordColumnValidator.ValidateFilterColumns(command.Filter.Root, container, null, null, _logger);
        if (!filterColumns.IsSuccess)
            return filterColumns.ToNewResult<int>();

        var mapper = PocoMapperCollection.ByName(command.Record.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.PocoMapperNotFound(_logger, _connectionName, command.Record.GetType().Name));

        var parameters = new Dictionary<string, object?>(mapper.MapToParameters(command.Record), StringComparer.OrdinalIgnoreCase);
        var setColumns = UpdatableColumns(container, parameters);
        if (setColumns.Count == 0)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.NoUpdatableColumns(
                    _logger, _connectionName, container.Name, command.Record.GetType().Name));

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        var rows = rowsResult.Value!.Select(CopyRow).ToList();
        var modified = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var row in rows)
        {
            if (!RecordRowMatcher.Matches(row, null, null, command.Filter.Root))
                continue;
            foreach (var column in setColumns)
                row[column] = parameters.TryGetValue(column, out var value) ? value : null;
            modified.Add(row);
        }

        if (modified.Count == 0)
        {
            FileSystemConfigurationWriteLog.UpdateCompleted(_logger, _connectionName, container.Name, 0);
            return GenericResult<int>.Success(0);
        }

        var validate = RecordRowValidator.Validate(modified, container, _logger);
        if (!validate.IsSuccess)
            return validate.ToNewResult<int>();

        var writeResult = await WriteRows(container, command.RelativePath, rows, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
            return writeResult;

        FileSystemConfigurationWriteLog.UpdateCompleted(_logger, _connectionName, container.Name, modified.Count);
        return GenericResult<int>.Success(modified.Count);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ConfigurationDelete — soft-delete via in-place update
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Soft-deletes the current row whose logical key equals the command's logical Id: sets
    /// <c>IsCurrent=false</c>/<c>IsDeleted=true</c> in place (no tombstone row added), then rewrites the
    /// whole file.
    /// </summary>
    public async Task<IGenericResult<int>> Delete(
        FileSystemConfigurationDeleteCommand command, CancellationToken cancellationToken = default)
    {
        var pathResult = ResolveAbsolutePath(command.RelativePath);
        if (!pathResult.IsSuccess)
            return pathResult.ToNewResult<int>();

        var fileLock = GetFileLock(pathResult.Value!);
        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await DeleteCore(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private async Task<IGenericResult<int>> DeleteCore(
        FileSystemConfigurationDeleteCommand command, CancellationToken cancellationToken)
    {
        var container = command.Container;
        FileSystemConfigurationWriteLog.DeleteStarting(_logger, _connectionName, container.Name);

        var logicalKeyColumn = LogicalKeyColumn(container);
        if (logicalKeyColumn is null)
            return GenericResult<int>.Failure(
                FileSystemConfigurationWriteLog.LogicalKeyNotFound(_logger, _connectionName, container.Name));

        var rowsResult = await _connector
            .ReadExistingOrEmpty(container, command.RelativePath, cancellationToken).ConfigureAwait(false);
        if (!rowsResult.IsSuccess)
            return rowsResult.ToNewResult<int>();

        var rows = rowsResult.Value!.Select(CopyRow).ToList();
        var modified = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var row in rows)
        {
            if (!KeyValuesEqual(GetCell(row, logicalKeyColumn), command.LogicalId) || !IsFlagTrue(GetCell(row, IsCurrentColumn)))
                continue;
            row[IsCurrentColumn] = false;
            row[IsDeletedColumn] = true;
            modified.Add(row);
        }

        if (modified.Count == 0)
        {
            FileSystemConfigurationWriteLog.DeleteCompleted(_logger, _connectionName, container.Name, 0);
            return GenericResult<int>.Success(0);
        }

        var validate = RecordRowValidator.Validate(modified, container, _logger);
        if (!validate.IsSuccess)
            return validate.ToNewResult<int>();

        var writeResult = await WriteRows(container, command.RelativePath, rows, cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
            return writeResult;

        FileSystemConfigurationWriteLog.DeleteCompleted(_logger, _connectionName, container.Name, modified.Count);
        return GenericResult<int>.Success(modified.Count);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ════════════════════════════════════════════════════════════════════════════

    private Task<IGenericResult<int>> WriteRows(
        IDataContainer container,
        string relativePath,
        IReadOnlyList<Dictionary<string, object?>> rows,
        CancellationToken cancellationToken)
        => _connector.Write(
            container,
            relativePath,
            rows.Cast<IReadOnlyDictionary<string, object?>>().ToList(),
            cancellationToken);

    private static void AddMappedColumns(
        Dictionary<string, object?> newRow,
        HashSet<string> propertyNames,
        Dictionary<string, object?> parameters,
        HashSet<string> insertable,
        string physicalKeyColumn)
    {
        foreach (var name in propertyNames)
        {
            // Translator-managed columns (physical RowId, version flags) are set explicitly, never from the POCO.
            if (IsManagedColumn(name, physicalKeyColumn) || !insertable.Contains(name))
                continue;
            newRow[name] = parameters.TryGetValue(name, out var value) ? value : null;
        }
    }

    // Why: merges IConfigurationSaveCommand.AdditionalColumnValues into the candidate parameter/property
    // sets before the container-field intersection, so an injected column (e.g. a KVP child's owner FK)
    // flows through the same pipeline as mapped columns — the same merge MsSql's translator performs.
    private static void MergeAdditionalColumns(
        IReadOnlyDictionary<string, object?> extra,
        Dictionary<string, object?> parameters,
        HashSet<string> propertyNames)
    {
        if (extra.Count == 0)
            return;
        foreach (var kv in extra)
        {
            parameters[kv.Key] = kv.Value;
            propertyNames.Add(kv.Key);
        }
    }

    private static bool IsManagedColumn(string name, string physicalKeyColumn) =>
        string.Equals(name, physicalKeyColumn, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, IsCurrentColumn, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, IsDeletedColumn, StringComparison.OrdinalIgnoreCase);

    // Why: the container's DECLARED field names are its IDataNode field children — read from Nodes, NOT
    // container.Schema. A FileSystem container's fields implement IDataField only (not IField), so accessing
    // container.Schema throws; Nodes is the schema-agnostic source every record-connector transport uses.
    // FileSystem field metadata carries no identity/computed/system flags (file records are schema-only),
    // so every declared field is insertable — the translator-managed columns are excluded downstream by
    // IsManagedColumn. Mirrors the intent of MsSqlConfigurationSaveTranslator.BuildInsertableFieldSet.
    private static HashSet<string> InsertableFieldNames(IDataContainer container)
    {
        var insertable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodes = container.Nodes;
        for (var i = 0; i < nodes.Count; i++)
            insertable.Add(nodes[i].Name);
        return insertable;
    }

    // Why: the SET column list for an Update — every declared field the record supplies a value for,
    // excluding the physical PK and the version flags (mirrors MsSqlUpdateTranslator's exclusions; the
    // version flags are excluded by name here because FileSystem field metadata carries no IsSystemProvided).
    private static List<string> UpdatableColumns(IDataContainer container, Dictionary<string, object?> parameters)
    {
        var physicalKeyColumn = PhysicalKeyColumn(container);
        var columns = new List<string>();
        var nodes = container.Nodes;
        for (var i = 0; i < nodes.Count; i++)
        {
            var name = nodes[i].Name;
            if (physicalKeyColumn is not null && string.Equals(name, physicalKeyColumn, StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(name, IsCurrentColumn, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, IsDeletedColumn, StringComparison.OrdinalIgnoreCase))
                continue;
            if (parameters.ContainsKey(name))
                columns.Add(name);
        }
        return columns;
    }

    // Why: the container's own durable logical identity — a Logical key that does NOT reference another
    // container (the table's Id), falling back to the first Foreign key for a typed body with no Id column.
    // Mirrors DefaultConfigurationProvider.FindKeyFieldName and MsSql's ResolveUpdatePredicate.
    private static string? LogicalKeyColumn(IDataContainer container)
    {
        var keys = container.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (string.Equals(key.KeyType.Name, "Logical", StringComparison.Ordinal)
                && key.ReferencedContainer is null && key.KeyFields.Count > 0)
                return key.KeyFields[0].LocalField.Name;
        }
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (string.Equals(key.KeyType.Name, "Foreign", StringComparison.Ordinal) && key.KeyFields.Count > 0)
                return key.KeyFields[0].LocalField.Name;
        }
        return null;
    }

    // Why: the physical version PK column (RowId) — the Physical key's field. Version RowId assignment and
    // FK resolution both key off this.
    private static string? PhysicalKeyColumn(IDataContainer container)
    {
        var keys = container.Keys;
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            if (string.Equals(key.KeyType.Name, "Physical", StringComparison.Ordinal) && key.KeyFields.Count > 0)
                return key.KeyFields[0].LocalField.Name;
        }
        return null;
    }

    private static long NextRowId(IReadOnlyList<Dictionary<string, object?>> rows, string physicalKeyColumn)
    {
        long max = 0;
        foreach (var row in rows)
        {
            var value = ToLong(GetCell(row, physicalKeyColumn));
            if (value is not null && value.Value > max)
                max = value.Value;
        }
        return max + 1;
    }

    private static Dictionary<string, object?> CopyRow(IReadOnlyDictionary<string, object?> row)
        => new(row, StringComparer.OrdinalIgnoreCase);

    private static object? GetCell(IReadOnlyDictionary<string, object?> row, string column)
        => row.TryGetValue(column, out var value) ? value : null;

    // Why: version/FK keys arrive from a decoded row as raw format primitives (a Guid column reads back as
    // a string, a RowId column as a long) while the write-side value is a native Guid/long — compare by
    // coercing to a common representation rather than requiring identical CLR types. Same tolerance the
    // shared RecordRowMatcher applies to filter values.
    private static bool KeyValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
            return false;

        var leftGuid = ToGuid(left);
        var rightGuid = ToGuid(right);
        if (leftGuid is not null && rightGuid is not null)
            return leftGuid.Value == rightGuid.Value;

        var leftLong = ToLong(left);
        var rightLong = ToLong(right);
        if (leftLong is not null && rightLong is not null)
            return leftLong.Value == rightLong.Value;

        return string.Equals(
            Convert.ToString(left, CultureInfo.InvariantCulture),
            Convert.ToString(right, CultureInfo.InvariantCulture),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFlagTrue(object? value) => value switch
    {
        null => false,
        bool flag => flag,
        string text when bool.TryParse(text, out var parsed) => parsed,
        string text when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number != 0,
        long integral => integral != 0,
        int integer => integer != 0,
        _ => false,
    };

    private static Guid? ToGuid(object value) => value switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out var parsed) => parsed,
        _ => null,
    };

    private static long? ToLong(object? value) => value switch
    {
        null => null,
        long integral => integral,
        int integer => integer,
        short small => small,
        byte b => b,
        string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null,
    };
}
