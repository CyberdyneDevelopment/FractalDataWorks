using System;
using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
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
    public string Name => TableName;

    /// <inheritdoc />
    public object Id => TableName;

    /// <inheritdoc />
    public string Category => "ConfigurationCommand";

    /// <inheritdoc />
    public Type ConfigType => typeof(TConfig);

    /// <inheritdoc />
    IDataCommand IConfigurationCommands.Create(string dataStoreName, string pathName, IGenericConfiguration record)
        => Create(dataStoreName, pathName, (TConfig)record);

    IDataCommand IConfigurationCommands.Create(string dataStoreName, string pathName, IGenericConfiguration record, IReadOnlyDictionary<string, object?> fieldValues)
        => Create(dataStoreName, pathName, (TConfig)record, fieldValues);

    IDataCommand IConfigurationCommands.Insert(string dataStoreName, string pathName, IGenericConfiguration record, IReadOnlyDictionary<string, object?> fieldValues)
        => Insert(dataStoreName, pathName, (TConfig)record, fieldValues);

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

    /// <summary>
    /// The column that holds this configuration's durable id.
    /// </summary>
    protected virtual string IdColumn => "Id";

    /// <summary>Creates a version-on-write save of a record whose fields are all mapped properties.</summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="pathName">The path (schema) name.</param>
    /// <param name="record">The record to save.</param>
    /// <returns>The save command.</returns>
    public virtual IDataCommand Create(string dataStoreName, string pathName, TConfig record)
        => new ConfigurationSaveCommand<TConfig>(record, replacesCurrent: true);

    /// <summary>
    /// Creates a version-on-write save of a record plus field values its properties do not carry --
    /// the Foreign key fields its owner fills with the owner's key values.
    /// </summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="pathName">The path (schema) name.</param>
    /// <param name="record">The record to save.</param>
    /// <param name="fieldValues">The field values written with the record, keyed by field name.</param>
    /// <returns>The save command.</returns>
    public virtual IDataCommand Create(string dataStoreName, string pathName, TConfig record, IReadOnlyDictionary<string, object?> fieldValues)
        => new ConfigurationSaveCommand<TConfig>(record, fieldValues, replacesCurrent: true);

    /// <summary>
    /// Creates an insert-only write of a record plus field values its properties do not carry: the row is
    /// written and nothing is retired.
    /// </summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="pathName">The path (schema) name.</param>
    /// <param name="record">The record to insert.</param>
    /// <param name="fieldValues">The field values written with the record, keyed by field name.</param>
    /// <returns>The insert command.</returns>
    /// <remarks>
    /// For a row that replaces nothing: a child written under its owner's new row, or a new version of a
    /// row whose previous version the caller has just retired with <see cref="Supersede"/>. Unlike
    /// the field values of <c>Create</c>, which also select the row it retires, these are only written.
    /// </remarks>
    public virtual IDataCommand Insert(string dataStoreName, string pathName, TConfig record, IReadOnlyDictionary<string, object?> fieldValues)
        => new ConfigurationSaveCommand<TConfig>(record, fieldValues, replacesCurrent: false);

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
    /// <param name="dataStoreName">DataStore name (e.g. "PlatformConfiguration").</param>
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

        if (asOf is null)
            builder = builder
                .Where(string.Concat(parentTable, ".IsCurrent"), true)
                .Where(string.Concat(parentTable, ".IsDeleted"), false);

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
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName).Where(IdColumn, id),
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

    /// <summary>Builds a query for one page of the current, non-deleted configuration records, in name order.</summary>
    /// <param name="dataStoreName">The data store to read from.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="skip">How many records precede the page.</param>
    /// <param name="take">How many records the page holds at most.</param>
    /// <remarks>
    /// Ordered by name because a page is only a page of something in a fixed order: without one, two
    /// requests for consecutive pages may overlap or skip rows. Name is unique among current records,
    /// so the order is total.
    /// </remarks>
    public virtual IDataCommand List(string dataStoreName, string pathName, int skip, int take)
    {
        return ApplyVersionFilter(
                new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName),
                null)
            .OrderBy(NameColumn)
            .Paging(skip, take)
            .Build().Command;
    }

    /// <summary>The name of the column a count query returns its number in.</summary>
    public const string CountColumn = "Total";

    /// <summary>Builds a query for one page of the current, non-deleted records a <see cref="DomainQuery"/> selects.</summary>
    /// <param name="dataStoreName">The data store to read from.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="query">The caller's filter, ordering and page.</param>
    /// <param name="keyFields">
    /// The container's declared key fields, read from its configuration by the provider. They complete
    /// the ordering, so two requests for consecutive pages cannot overlap or skip a row.
    /// </param>
    /// <remarks>
    /// The version predicates are always present and the caller's filter is ANDed to them: a query can
    /// narrow what is current, never reach a retired or deleted row. With no ordering asked for, the
    /// records come in name order, as <c>List</c> returns them. The page asks for one row past the
    /// take: that row is how the page knows another follows, without counting.
    /// </remarks>
    public virtual IDataCommand Get(string dataStoreName, string pathName, DomainQuery query, IReadOnlyList<string> keyFields)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keyFields);

        return new QueryCommand<TConfig>
        {
            Filter = QueryFilter(query),
            Ordering = QueryOrdering(query, keyFields),
            Paging = new PagingExpression { Skip = query.Skip, Take = query.Take + 1 },
        };
    }

    /// <summary>Builds a query that counts the current, non-deleted records a <see cref="DomainQuery"/> selects.</summary>
    /// <param name="dataStoreName">The data store to read from.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="query">The caller's filter; its ordering and page do not apply to a count.</param>
    /// <returns>A query whose one row carries the number in <see cref="CountColumn"/>.</returns>
    public virtual IDataCommand Count(string dataStoreName, string pathName, DomainQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return new QueryCommand<Dictionary<string, object?>>
        {
            Filter = QueryFilter(query),
            Aggregation = new AggregationExpression
            {
                GroupByFields = [],
                Aggregations = new Dictionary<string, string>(StringComparer.Ordinal) { [CountColumn] = "COUNT(*)" },
            },
        };
    }

    /// <summary>Creates a retire, not a delete, of the current row whose durable id is <paramref name="id"/>.</summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="pathName">The path (schema) name.</param>
    /// <param name="id">The durable id of the row whose current version is retired.</param>
    /// <remarks>
    /// For replacing a row with a new version of itself when the new version is written by
    /// <c>Insert</c>. The row stops being current and is not marked deleted, so it stays in the row's
    /// history as what the row used to be. <c>Delete</c> is the retire that also marks it deleted.
    /// </remarks>
    public virtual IDataCommand Supersede(string dataStoreName, string pathName, Guid id)
        => new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [id], markDeleted: false);

    // The caller's filter narrows the current rows; it never widens them.
    private static FilterExpression QueryFilter(DomainQuery query)
    {
        var nodes = new List<IFilterNode>
        {
            new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
            new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
        };
        if (query.Filter?.Root is { } asked)
            nodes.Add(asked);

        return new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes } };
    }

    // The order the caller asked for, or name order; then every key field not already ordered on.
    private OrderingExpression QueryOrdering(DomainQuery query, IReadOnlyList<string> keyFields)
    {
        var ascending = SortDirections.ByName("Ascending");
        var ordered = new List<IOrderedField>();
        if (query.Ordering is { OrderedFields.Count: > 0 } asked)
            ordered.AddRange(asked.OrderedFields);
        else
            ordered.Add(new OrderedField { PropertyName = NameColumn, Direction = ascending });

        foreach (var key in keyFields)
        {
            var alreadyOrdered = false;
            foreach (var field in ordered)
            {
                if (string.Equals(field.PropertyName, key, StringComparison.Ordinal))
                {
                    alreadyOrdered = true;
                    break;
                }
            }

            if (!alreadyOrdered)
                ordered.Add(new OrderedField { PropertyName = key, Direction = ascending });
        }

        return new OrderingExpression { OrderedFields = ordered };
    }


    /// <summary>Creates a delete of the record whose durable id is <paramref name="id"/>.</summary>
    /// <remarks>
    /// The id is the value of the key the container declares as Logical; the translator reads which
    /// fields that key names from the container's configuration.
    /// </remarks>
    public virtual IDataCommand Delete(string dataStoreName, string pathName, Guid id)
        => new ConfigurationRetireCommand((KeyTypeBase)KeyTypes.ByName("Logical"), [id], markDeleted: true);

    /// <summary>Creates a retire of every current row whose fields hold <paramref name="fieldValues"/>.</summary>
    public virtual IDataCommand Retire(string dataStoreName, string pathName, IReadOnlyDictionary<string, object?> fieldValues, bool markDeleted)
        => new ConfigurationRetireCommand(fieldValues, markDeleted);

    /// <summary>Builds a query returning all versions of a configuration record in descending order.</summary>
    public virtual IDataCommand ViewHistory(string dataStoreName, string pathName, Guid id)
        => new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName)
            .Where(IdColumn, id)
            .OrderByDescending("ModifyDate")
            .Build()
            .Command;

    /// <summary>Builds a query that checks whether a current record with the given Id exists.</summary>
    public virtual IDataCommand Validate(string dataStoreName, string pathName, TConfig record)
        => new QueryCommandBuilder<TConfig>(dataStoreName, pathName, TableName)
            .Where(IdColumn, record.Id)
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
