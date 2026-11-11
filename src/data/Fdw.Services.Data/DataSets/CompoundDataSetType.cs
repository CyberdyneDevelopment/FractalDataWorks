using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using IDataField = Fdw.Data.DataSets.Abstractions.IDataField;

namespace Fdw.Services.Data;

/// <summary>
/// Compound dataset strategy: sources within a SINGLE store (a database) whose join is PUSHED DOWN
/// to the store as one query.
/// </summary>
/// <remarks>
/// Why: registered as the <c>"Compound"</c> member of <see cref="DataSetTypes"/>; selected when a
/// dataset's authored <c>Implementation</c> is <c>"Compound"</c>. The defining property is that the
/// join is performed by the backend (not in memory), so all sources must live in one store.
/// <para>
/// Builds a single <see cref="QueryCommand{T}"/> carrying JOIN expressions and a multi-source
/// projection (each field qualified with its owning container name). The regular QueryTranslator
/// renders this into a pushed-down JOIN SELECT statement. No in-memory join. No fallback.
/// </para>
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Compound")]
public sealed class CompoundDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CompoundDataSetType"/> class.</summary>
    public CompoundDataSetType()
        : base("Compound", "Single-store dataset whose join is pushed down to the store",
            typeof(object), Array.Empty<IDataField>(), category: "DataSetStrategy")
    {
    }

    /// <inheritdoc />
    public override IDataQuery CreateQuery() => new DataQueryBuilder<object>(Name);

    /// <inheritdoc />
    /// <remarks>
    /// One store, one query: the join, the filter, the order and the page are all that query's (FDW-4991). A page follows the caller's
    /// order with the data set's key fields as a tie-break, else the key fields, so a join that repeats its primary rows still gives every
    /// row one place. Grouping stays refused: the translators take no aggregation over a join.
    /// </remarks>
    public override bool PagesInStore => true;

    /// <inheritdoc />
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default)
    {
        if (context is not DataSetExecutionContext ctx)
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetConfigurationRequired"));

        if (command is IQueryCommand { Aggregation: not null })
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("DataSetNotGroupable"), ctx.Logger,
                ResultDetails.Create("dataSet", ctx.Config.Name, "reason", "a Compound data set builds its rows from more than one read, so it has no one query to group"));

        if (ctx.Config is not ICompoundDataSetConfiguration compound)
            return DataSetExecutionHelpers.KindMismatch<T>(ctx, "Compound");

        // Every column the joined query reads is one its mappings name; a set stored before FDW-5022 that leaves one to a guess is refused.
        var mapped = CompoundMappingRules.Validate(compound, ctx.Logger);
        if (mapped.IsFailure)
            return mapped.ToNewResult<T>();

        var sources = ctx.Config.Sources;
        if (sources is null || sources.Count == 0)
            return GenericResult<T>.Failure(DataGatewayLogger.DataSetNoSources(ctx.Logger, ctx.Config.Name));

        var storeCount = sources.Select(s => s.DataStoreName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (storeCount > 1)
            return GenericResult<T>.Failure(
                DataGatewayLogger.CompoundSourcesSpanStores(ctx.Logger, ctx.Config.Name, storeCount));

        DataGatewayLogger.ExecutingCompoundDataSet(ctx.Logger, ctx.Config.Name, sources.Count);

        // Primary source is the FROM table: lowest Priority wins (ties keep declaration order).
        var primarySource = sources.OrderBy(s => s.Priority).First();
        var primaryContainerName = DataSetExecutionHelpers.GetContainerName(primarySource);
        if (primaryContainerName == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceNoContainer(ctx.Logger, primarySource.SourceName, ctx.Config.Name));

        var (owners, ownersError) = FieldOwners<T>(ctx, sources);
        if (ownersError is not null) return ownersError;

        var joinResult = BuildJoinExpressions<T>(ctx, compound.Joins, sources, primaryContainerName);
        if (joinResult.HasError) return joinResult.ErrorResult!;

        if (KeyOnOptionalSide(ctx, command, owners, compound.Joins, primarySource.SourceName) is { } optionalKey)
            return GenericResult<T>.Failure(DataGatewayLogger.CompoundKeyOnOptionalSide(ctx.Logger, ctx.Config.Name, optionalKey));

        var read = ReadExpressions<T>(ctx, command, owners);
        if (read.ErrorResult is { } readError) return readError;

        var sourceQuery = new QueryCommand<T>
        {
            Joins = joinResult.Joins!,
            Projection = Projection(owners),
            Filter = read.Filter,
            Ordering = read.Ordering,
            Paging = (command as IQueryCommand)?.Paging,
        };
        return await ExecuteCompoundQuery(ctx, sourceQuery, primarySource, primaryContainerName, ct).ConfigureAwait(false);
    }

    // The container and physical column of the source that holds a field, as the joined query names it.
    private sealed record FieldOwner(string Field, string SourceName, string Container, string Physical)
    {
        public string Qualified => string.Concat(Container, ".", Physical);
    }

    // Which source holds each of the data set's stored fields: the one source whose field mappings name it, which CompoundMappingRules has
    // already checked (FDW-5022). Calculated fields have no column; they are applied after the read.
    private static (List<FieldOwner> Owners, IGenericResult<T>? ErrorResult) FieldOwners<T>(
        DataSetExecutionContext ctx,
        IList<DataSetSourceConfiguration> sources)
    {
        var owners = new List<FieldOwner>();
        foreach (var field in ctx.Config.Fields.Where(f => f.IsCurrent && !f.IsDeleted && !f.IsCalculated))
        {
            var owningSource = sources.First(source => source.FieldMappings.ContainsKey(field.Name));
            var physicalColumn = owningSource.FieldMappings[field.Name];
            var sourceContainerName = DataSetExecutionHelpers.GetContainerName(owningSource);
            if (sourceContainerName == null)
                return ([], GenericResult<T>.Failure(
                    DataGatewayLogger.SourceNoContainer(ctx.Logger, owningSource.SourceName, ctx.Config.Name)));

            owners.Add(new FieldOwner(field.Name, owningSource.SourceName, sourceContainerName, physicalColumn));
        }

        return (owners, null);
    }

    // Every stored field, each qualified with its source's container and aliased to the data set's name for it.
    private static ProjectionExpression? Projection(List<FieldOwner> owners) =>
        owners.Count == 0
            ? null
            : new ProjectionExpression
            {
                Fields = [.. owners.Select(owner => new ProjectionField { PropertyName = owner.Physical, Alias = owner.Field, SourceContainer = owner.Container })],
            };

    private static (bool HasError, List<IJoinExpression>? Joins, IGenericResult<T>? ErrorResult) BuildJoinExpressions<T>(
        DataSetExecutionContext ctx,
        IList<DataSetJoinConfiguration> joins,
        IList<DataSetSourceConfiguration> sources,
        string primaryContainerName)
    {
        var sourceLookup = sources.ToDictionary(s => s.SourceName, StringComparer.OrdinalIgnoreCase);
        var joinExpressions = new List<IJoinExpression>(joins.Count);

        foreach (var join in joins.OrderBy(j => j.Ordinal))
        {
            if (!sourceLookup.TryGetValue(join.LeftSourceName, out var leftSource))
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.LeftSourceName)));

            if (!sourceLookup.TryGetValue(join.RightSourceName, out var rightSource))
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.RightSourceName)));

            var leftContainerName = DataSetExecutionHelpers.GetContainerName(leftSource);
            if (leftContainerName == null)
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.SourceNoContainer(ctx.Logger, leftSource.SourceName, ctx.Config.Name)));

            var rightContainerName = DataSetExecutionHelpers.GetContainerName(rightSource);
            if (rightContainerName == null)
                return (true, null, GenericResult<T>.Failure(
                    DataGatewayLogger.SourceNoContainer(ctx.Logger, rightSource.SourceName, ctx.Config.Name)));

            joinExpressions.Add(new JoinExpression
            {
                TargetContainerName = rightContainerName,
                // A join whose left side is not the primary source continues from the container an earlier join brought in (FDW-4991).
                SourceContainerName = string.Equals(leftContainerName, primaryContainerName, StringComparison.Ordinal) ? null : leftContainerName,
                // The join type's own option, by its name; the translator states it by the option's SQL keyword, never by the stored text
                // (FDW-5099). A type with no condition (Cross) takes no join columns.
                JoinType = StatedJoinType.Of(join.JoinType).Name,
                // Both columns are mapped: CompoundMappingRules refused a join column its source does not map (FDW-5022).
                JoinConditions = StatedJoinType.Of(join.JoinType).RequiresConditions
                    ? [(leftSource.FieldMappings[join.LeftFieldName], rightSource.FieldMappings[join.RightFieldName])]
                    : [],
            });
        }

        return (false, joinExpressions, null);
    }

    /// <summary>The first key field of a page that an outer join may leave without a value, or null when every key field always has one (FDW-5024).</summary>
    /// <remarks>
    /// A page is cut by its key, and a keyset page asks for the rows after the last key, so a key that can be NULL repeats or skips rows.
    /// A source is optional when a join keeps every row of its other side: the right source of a join that keeps every left row, and every
    /// source joined before a join that keeps every right row. Which side a join keeps is its join type's own statement.
    /// </remarks>
    private static string? KeyOnOptionalSide(
        DataSetExecutionContext ctx, IDataCommand command, List<FieldOwner> owners, IList<DataSetJoinConfiguration> joins, string primarySourceName)
    {
        if ((command as IQueryCommand)?.Paging is null)
            return null;

        var joined = new List<string> { primarySourceName };
        var optional = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var join in joins.OrderBy(j => j.Ordinal))
        {
            var kind = StatedJoinType.Of(join.JoinType);
            if (kind.IncludesAllRight)
                optional.UnionWith(joined);
            if (kind.IncludesAllLeft)
                optional.Add(join.RightSourceName);
            joined.Add(join.RightSourceName);
        }

        return ctx.Config.Fields
            .Where(field => field.IsCurrent && !field.IsDeleted && field.IsKey && !field.IsCalculated)
            .Select(field => field.Name)
            .FirstOrDefault(key => owners.Any(owner =>
                string.Equals(owner.Field, key, StringComparison.OrdinalIgnoreCase) && optional.Contains(owner.SourceName)));
    }

    /// <summary>The read's filter and order in the joined query's names: each field qualified with the container of the source that holds it.</summary>
    /// <remarks>
    /// The order is <see cref="DataSetKeyOrder.For"/>: the caller's with the key fields as a tie-break, else the key fields when the read
    /// is a page. A field none of the sources holds is refused, never sent to the primary table (FDW-4991).
    /// </remarks>
    private static (IFilterExpression? Filter, IOrderingExpression? Ordering, IGenericResult<T>? ErrorResult) ReadExpressions<T>(
        DataSetExecutionContext ctx, IDataCommand command, List<FieldOwner> owners)
    {
        var ordering = DataSetKeyOrder.For(ctx.Config, command, ctx.Logger);
        if (ordering.IsFailure)
            return (null, null, ordering.ToNewResult<T>());

        var qualified = owners
            .GroupBy(owner => owner.Field, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Qualified, StringComparer.OrdinalIgnoreCase);
        // The data set's own filter ANDed with the caller's (FDW-4997), qualified like any filter of the joined query.
        var readFilter = DataSetReadFilter.For(ctx, command);
        if (readFilter.IsFailure)
            return (null, null, readFilter.ToNewResult<T>());

        var filter = readFilter.Value;

        var unowned = UnownedField(filter?.Root, qualified);
        if (unowned is null && ordering.Value is { } stated)
            unowned = stated.OrderedFields.Select(field => field.PropertyName).FirstOrDefault(name => !qualified.ContainsKey(name));
        if (unowned is not null)
            return (null, null, GenericResult<T>.Failure(DataGatewayLogger.CompoundFieldNotOwned(ctx.Logger, ctx.Config.Name, unowned)));

        if (filter is null)
            return (null, PredicatePushdownAnalyzer.TranslateOrderingToPhysical(ordering.Value, qualified), null);

        var translated = ctx.Pushdown.TranslateToPhysical(filter, qualified);
        return translated.IsSuccess
            ? (translated.Value, PredicatePushdownAnalyzer.TranslateOrderingToPhysical(ordering.Value, qualified), null)
            : (null, null, GenericResult<T>.Failure(
                DataGatewayLogger.FilterTranslationFailed(ctx.Logger, ctx.Config.Name, translated.FailureReason().Message)));
    }

    // The first field a filter names that no source holds, or null when every one is held.
    private static string? UnownedField(IFilterNode? node, IReadOnlyDictionary<string, string> qualified) => node switch
    {
        IFilterCondition condition => qualified.ContainsKey(condition.PropertyName) ? null : condition.PropertyName,
        FilterGroup group => group.Nodes.Select(child => UnownedField(child, qualified)).FirstOrDefault(name => name is not null),
        _ => null,
    };

    private static async Task<IGenericResult<T>> ExecuteCompoundQuery<T>(
        DataSetExecutionContext ctx,
        QueryCommand<T> sourceQuery,
        DataSetSourceConfiguration primarySource,
        string primaryContainerName,
        CancellationToken ct)
    {
        // The joined containers are resolved with the primary: the translator finds a join target among the containers resolved with it
        // (FDW-4328), as the gateway resolves them for any command that joins.
        var joined = ContainersJoined.By(sourceQuery, primaryContainerName);
        var containerResult = await (joined.Count == 0
                ? ctx.DataStoreProvider.Get(primarySource.DataStoreName, primarySource.PathValue, primaryContainerName, ct)
                : ctx.DataStoreProvider.Get(primarySource.DataStoreName, primarySource.PathValue, primaryContainerName, joined, ct))
            .ConfigureAwait(false);
        if (!containerResult.IsSuccess || containerResult.Value == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, primarySource.SourceName));

        if (containerResult.Value.Parent?.Store?.ConnectionId is not { } connectionId || connectionId == Guid.Empty)
            return GenericResult<T>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, primarySource.DataStoreName,
                    "the container's DataStore carries no ConnectionId"));

        var connectionResult = await ctx.ConnectionProvider.Get(connectionId, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
            return ConnectionFailures.Caused<T>(
                connectionResult, DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, primarySource.DataStoreName, connectionResult.NoValueReason().Message));

        // Execute the pushed-down compound query through the single connection.
        var result = await connectionResult.Value.Execute<T>(sourceQuery, containerResult.Value, ct).ConfigureAwait(false);

        return await ShapeRows(ctx, result, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Shapes the joined rows. The projection aliases every column to its logical name, so each
    /// source's attached transformation chains apply to the rows directly (FDW-877), then calculated
    /// fields read the transformed values.
    /// </summary>
    private static async Task<IGenericResult<T>> ShapeRows<T>(
        DataSetExecutionContext ctx, IGenericResult<T> result, CancellationToken ct)
    {
        if (result.IsFailure || result.Value == null)
            return result;

        var chains = await DataSetExecutionHelpers.ResolveChains(ctx, ctx.Config.Sources, ct).ConfigureAwait(false);
        if (chains.IsFailure)
            return chains.ToNewResult<T>();
        if (chains.Value!.Count > 0)
            result = await DataSetExecutionHelpers.ApplyFieldTransformations(ctx, result, chains.Value, ctx.Config.Name, ct).ConfigureAwait(false);

        var calculatedFields = ctx.Config.Fields.Where(f => f.IsCalculated).ToList();
        if (calculatedFields.Count > 0 && result.IsSuccess && result.Value != null)
        {
            DataGatewayLogger.ApplyingCalculatedFields(ctx.Logger, calculatedFields.Count, ctx.Config.Name);
            result = DataSetExecutionHelpers.ApplyCalculatedFields(ctx, result, calculatedFields, ctx.Config.Name);
        }

        return result;
    }
}
