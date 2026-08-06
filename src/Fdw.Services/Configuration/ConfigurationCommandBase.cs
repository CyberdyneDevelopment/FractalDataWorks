using System;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Abstract base for configuration command TypeCollection options. Concrete subclasses declare
/// their <see cref="TableName"/> and inherit the default verbs that build standard
/// Query/Insert/Update/Delete commands against <c>{dataStoreName}.{pathName}.{TableName}</c>
/// with <c>IsCurrent=1 AND IsDeleted=0</c> filters and optional cascade.
/// </summary>
/// <typeparam name="TConfig">The concrete configuration type this command targets.</typeparam>
/// <remarks>
/// The <c>dataStoreName</c> and <c>pathName</c> are passed into each verb at call time (by the
/// provider) rather than stored on the instance, so a single Commands TypeOption instance can
/// serve multiple DataStores (primary + read-replica, ConfigurationDb vs AuthDb, etc).
/// </remarks>
public abstract class ConfigurationCommandBase<TConfig> : IConfigurationCommands
    where TConfig : class, IGenericConfiguration
{
    /// <summary>Gets the table name this command targets.</summary>
    public string TableName { get; }

    /// <inheritdoc />
    public string ContainerName => TableName;

    /// <inheritdoc />
    public Type ConfigType => typeof(TConfig);

    /// <inheritdoc />
    // Why: non-generic save-cascade entry point — casts the runtime-typed child record to this
    // command's concrete config type and forwards to the typed Create, so the cascade saves a child
    // whose type is only known at runtime without MakeGenericMethod.
    IDataCommand IConfigurationCommands.Create(string dataStoreName, string pathName, IGenericConfiguration record)
        => Create(dataStoreName, pathName, (TConfig)record);

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationCommandBase{TConfig}"/> with the table name.
    /// </summary>
    protected ConfigurationCommandBase(string tableName)
    {
        TableName = tableName;
    }

    /// <summary>
    /// The column that holds this configuration's natural-key name, used by Get(name). Defaults to "Name";
    /// override when the identity column differs (e.g. catalog.DataSetAnnotation keys on "DataSetName").
    /// </summary>
    protected virtual string NameColumn => "Name";

    /// <summary>Creates a configuration save command (version-on-write transaction with FK subquery resolution).</summary>
    /// <remarks>
    /// Why: ConfigurationSaveCommand routes through MsSqlConfigurationSaveTranslator which (a) writes
    /// the IsCurrent/IsDeleted audit columns automatically and (b) resolves physical FK columns
    /// (e.g. ConnectionRowId → parent.RowId) via subquery using container metadata. A plain
    /// InsertCommand would skip both — typed-body INSERTs then fail 515 because ConnectionRowId
    /// can't be sourced from the POCO.
    /// </remarks>
    public virtual IDataCommand Create(string dataStoreName, string pathName, TConfig record)
        => new ConfigurationSaveCommand<TConfig>(record);

    /// <summary>
    /// Applies the version predicate: the current version, or the version in force at
    /// <paramref name="asOf"/> when an instant is supplied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why one helper rather than a filter per verb: "which version" is a single decision that every
    /// read has to make identically. Spread across six call sites it drifts — one verb starts
    /// answering as-of and another keeps answering current, and a restatement silently mixes
    /// definitions from two different points in time.
    /// </para>
    /// <para>
    /// The as-of branch deliberately drops the <c>IsCurrent</c> filter — that filter is precisely
    /// what pins a read to the present, so keeping it would make every historical query return
    /// either today's row or nothing. <c>IsDeleted</c> stays: a deleted record is not "the version
    /// in force", it is a record that was withdrawn.
    /// </para>
    /// <para>
    /// The interval is half-open — <c>EffectiveStart &lt;= asOf &lt; EffectiveEnd</c>, with a NULL
    /// end meaning still in force. Half-open is what makes consecutive versions tile the timeline
    /// without overlapping: a version ending at midnight and the next starting at midnight yields
    /// exactly one row for that instant, where a closed interval would return both and force the
    /// caller to break the tie.
    /// </para>
    /// </remarks>
    protected static QueryCommandBuilder<TConfig> ApplyVersionFilter(
        QueryCommandBuilder<TConfig> builder,
        DateTimeOffset? asOf)
    {
        if (asOf is null)
        {
            return builder
                .Where("IsCurrent", true)
                .Where("IsDeleted", false);
        }

        return builder
            .Where("EffectiveStart", FilterOperators.ByName("LessThanOrEqual"), asOf.Value)
            .BeginOrGroup()
                .Where("EffectiveEnd", FilterOperators.ByName("GreaterThan"), asOf.Value)
                .Where("EffectiveEnd", FilterOperators.ByName("IsNull"), null)
            .EndGroup()
            .Where("IsDeleted", false);
    }

    /// <summary>Builds a query for a configuration record by name.</summary>
    /// <param name="dataStoreName">The data store to read from.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="name">The configuration's name.</param>
    /// <param name="asOf">
    /// When supplied, returns the version in force at that instant instead of the current version.
    /// Only meaningful for configurations declared <c>Temporal</c>.
    /// </param>
    public virtual IDataCommand Get(string dataStoreName, string pathName, string name, DateTimeOffset? asOf = null)
    {
        // Why: Build() returns DataGatewayCall; .Command extracts the address-free command.
        // The provider pairs it with Target via DefaultConfigurationProvider.Target.
        return ApplyVersionFilter(
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName).Where(NameColumn, name),
                asOf)
            .Build().Command;
    }

    /// <summary>
    /// Builds a Logical-key parented query: WHERE [{ParentLogicalIdColumn}] = @value AND IsCurrent=1 AND IsDeleted=0.
    /// Logical FK columns reference the parent's durable Id (e.g. ConnectionId → Connection.Id).
    /// IsCurrent filter is mandatory because the same logical Id can appear in multiple version rows.
    /// </summary>
    // Why: protected — only DefaultConfigurationProvider.Get(id) calls this when dispatching on the
    // registered parentKeyType=Logical. Not a public sidecar — column name comes from registration.
    protected internal virtual IDataCommand GetByParent(string dataStoreName, string pathName, string parentIdColumn, Guid parentId, DateTimeOffset? asOf = null)
    {
        return ApplyVersionFilter(
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName).Where(parentIdColumn, parentId),
                asOf)
            .Build().Command;
    }

    /// <summary>
    /// Builds a Physical-key parented query: WHERE [{ParentPhysicalRowIdColumn}] = @value.
    /// Physical FK columns reference the parent's version-specific RowId (e.g. ConnectionRowId → Connection.RowId).
    /// No IsCurrent filter — RowId is unique per version row, so the predicate is already version-specific.
    /// </summary>
    // Why: protected — only DefaultConfigurationProvider.Get(id) calls this when dispatching on the
    // registered parentKeyType=Physical. Not a public sidecar.
    protected internal virtual IDataCommand GetByPhysicalParent(string dataStoreName, string pathName, string parentRowIdColumn, Guid parentRowId)
    {
        return new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName)
            .Where(parentRowIdColumn, parentRowId)
            .Where("IsDeleted", false)
            .Build().Command;
    }

    /// <summary>
    /// Builds a typed-body read by joining the child table to its parent on the foreign key, filtered
    /// by the parent's durable Id. The join column names are supplied explicitly by the caller (the
    /// configuration provider reads them from the container's FK metadata) — this verb never reads
    /// metadata itself.
    /// </summary>
    /// <param name="dataStoreName">DataStore name (e.g. "ConfigurationDb").</param>
    /// <param name="pathName">Schema/path name (e.g. "sec", "conn").</param>
    /// <param name="childForeignKeyColumn">The child's FK column (e.g. "CredentialServiceRowId").</param>
    /// <param name="parentTable">The parent table to join to (e.g. "CredentialService").</param>
    /// <param name="parentJoinColumn">The parent column the FK references — its physical PK (e.g. "RowId").</param>
    /// <param name="parentKeyColumn">The parent's durable-Id column to filter on (e.g. "Id").</param>
    /// <param name="parentKeyValue">The parent's durable Id value.</param>
    /// <param name="asOf">
    /// When supplied, returns the child versions in force at that instant instead of the current
    /// ones. Only meaningful for configurations declared <c>Temporal</c>.
    /// </param>
    // Why: the declared FK is physical (child.{Parent}RowId → parent.RowId) and the parent's RowId is
    // NOT projected onto the header object, so we cannot filter the child by a known RowId. Instead we
    // JOIN child→parent on the FK and filter the parent by its durable Id (which IS materialized). The
    // child filters (IsCurrent/IsDeleted) stay bare → the translator qualifies them to the child table;
    // the parent filter is dotted "{parentTable}.{parentKeyColumn}" → qualified to the parent table.
    protected internal virtual IDataCommand GetByParentJoin(
        string dataStoreName,
        string pathName,
        string childForeignKeyColumn,
        string parentTable,
        string parentJoinColumn,
        string parentKeyColumn,
        Guid parentKeyValue,
        DateTimeOffset? asOf = null)
    {
        var builder = ApplyVersionFilter(
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName)
                    .Join(parentTable, childForeignKeyColumn, parentJoinColumn),
                asOf)
            .Where(string.Concat(parentTable, ".", parentKeyColumn), parentKeyValue);

        // Why: the FK points at a VERSION-SPECIFIC parent RowId, and the parent's durable Id matches every
        // version of it — so without this the join also matches bodies hanging off retired parent versions,
        // and a current read could return a stale body for a current header. Only on the current-version
        // read: an as-of read is already pinned by the child's own as-of predicate above, and pinning the
        // parent to IsCurrent there would defeat it. Mirrors BuildChildJoinQuery, which filters the owner
        // the same way for collection children.
        if (asOf is null)
            builder = builder.Where(string.Concat(parentTable, ".IsCurrent"), true);

        return builder.Build().Command;
    }

    /// <summary>Builds a query for a configuration record by id.</summary>
    /// <param name="dataStoreName">The data store to read from.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="id">The configuration's durable logical Id.</param>
    /// <param name="asOf">
    /// When supplied, returns the version in force at that instant instead of the current version.
    /// Only meaningful for configurations declared <c>Temporal</c>.
    /// </param>
    public virtual IDataCommand Get(string dataStoreName, string pathName, Guid id, DateTimeOffset? asOf = null)
    {
        return ApplyVersionFilter(
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName).Where("Id", id),
                asOf)
            .Build().Command;
    }

    /// <summary>Builds a query for all current, non-deleted configuration records.</summary>
    /// <param name="dataStoreName">The data store to read from.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="asOf">
    /// When supplied, returns the versions in force at that instant instead of the current ones.
    /// Only meaningful for configurations declared <c>Temporal</c>.
    /// </param>
    public virtual IDataCommand List(string dataStoreName, string pathName, DateTimeOffset? asOf = null)
    {
        return ApplyVersionFilter(
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName),
                asOf)
            .Build().Command;
    }

    // Why there is no Update verb: configuration tables are version-on-write, and Create IS the update —
    // its translator retires the current row (IsCurrent=0) and inserts the new version in one transaction,
    // which is correct for the first write and every later one. The in-place UPDATE that used to live here
    // was a second, incompatible write path: it minted no version, carried no IsCurrent predicate (so it
    // rewrote every historical row of the record), let the POCO's own IsCurrent/IsDeleted values reach the
    // columns, and — because the provider only cascaded on the insert branch — silently dropped the typed
    // body and every child. Deleted rather than deprecated: one write, one shape, every configuration type.

    /// <summary>Creates a DELETE command for a configuration record by id (soft delete).</summary>
    // Why: configuration tables are version-on-write with child-table FKs (e.g. MsSqlConnection
    // references conn.Connection.RowId). A plain DELETE FROM hits FK_*_Connection and 500s.
    // ConfigurationDeleteCommand is recognized by MsSqlConfigurationDeleteTranslator which UPDATEs
    // IsCurrent=0 + INSERTs a tombstone row, leaving FKs intact.
    public virtual IDataCommand Delete(string dataStoreName, string pathName, Guid id)
        => new ConfigurationDeleteCommand(id);

    /// <summary>Builds a query returning all versions of a configuration record in descending order.</summary>
    public virtual IDataCommand ViewHistory(string dataStoreName, string pathName, Guid id)
        => new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName)
            .Where("Id", id)
            .OrderByDescending("ModifyDate")
            .Build()
            .Command;

    /// <summary>Builds a query that checks whether a current record with the given Id exists.</summary>
    public virtual IDataCommand Validate(string dataStoreName, string pathName, TConfig record)
        => new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName)
            .Where("Id", record.Id)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build()
            .Command;

    /// <summary>
    /// Returns the cache invalidation tag for this command's table within the given schema path.
    /// Format: "{pathName}.{TableName}" — matches <c>CachePolicy.GetKeyPrefix</c> default convention.
    /// </summary>
    public string CacheTag(string pathName) => string.Concat(pathName, ".", TableName);
}
