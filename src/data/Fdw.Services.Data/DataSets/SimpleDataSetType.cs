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
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using IDataField = Fdw.Data.DataSets.Abstractions.IDataField;

namespace Fdw.Services.Data;

/// <summary>
/// Single-source dataset strategy: a direct pull from one source (with field rename and calculated
/// fields), and the writable sink path (Insert/Update/Delete forwarded verbatim to
/// the one source's container).
/// </summary>
/// <remarks>
/// Why: registered as the <c>"Simple"</c> member of <see cref="DataSetTypes"/>; selected when a
/// dataset's authored <c>Implementation</c> is <c>"Simple"</c>. The type option is a module-init
/// singleton with a parameterless constructor and no DI — it is stateless and reads everything it needs
/// for one execution from the <see cref="DataSetExecutionContext"/>.
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Simple")]
public sealed class SimpleDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SimpleDataSetType"/> class.</summary>
    public SimpleDataSetType()
        : base("Simple", "Single-source dataset (direct pull; also the writable sink)",
            typeof(object), Array.Empty<IDataField>(), category: "DataSetStrategy")
    {
    }

    /// <inheritdoc />
    public override IDataQuery CreateQuery() => new DataQueryBuilder<object>(Name);

    /// <inheritdoc />
    /// <remarks>
    /// A single source pushes ordering and paging into the store's own query. An aggregated Simple
    /// set does not: the aggregate translator takes no paging, so callers must still check for
    /// aggregates (FDW-876).
    /// </remarks>
    public override bool PagesInStore => true;

    /// <inheritdoc />
    /// <remarks>One source, one query: a grouping is that query's GROUP BY, when the source's connection runs one.</remarks>
    public override bool AggregatesInStore => true;

    /// <inheritdoc />
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default)
    {
        if (context is not DataSetExecutionContext ctx)
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetConfigurationRequired"));

        var sources = ctx.Config.Sources;
        if (sources is null || sources.Count == 0)
            return GenericResult<T>.Failure(DataGatewayLogger.DataSetNoSources(ctx.Logger, ctx.Config.Name));

        var isWrite = command is IDataCommandWithInput or DeleteCommand;

        if (sources.Count != 1)
        {
            return isWrite
                ? GenericResult<T>.Failure(
                    DataGatewayLogger.DataSetWriteRequiresSingleSource(ctx.Logger, command.GetType().Name, ctx.Config.Name, sources.Count))
                : GenericResult<T>.Failure(
                    DataGatewayLogger.DataSetValidationFailed(ctx.Logger, ctx.Config.Name, $"Simple dataset requires exactly one source but has {sources.Count}"));
        }

        DataGatewayLogger.ExecutingSimpleDataSet(ctx.Logger, ctx.Config.Name);
        var sourceConfig = sources[0];

        return isWrite
            ? await ExecuteSourceQuery<T>(ctx, command, sourceConfig, ct).ConfigureAwait(false)
            : await ExecuteRead<T>(ctx, command, sourceConfig, ct).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<T>> ExecuteRead<T>(
        DataSetExecutionContext ctx, IDataCommand command, DataSetSourceConfiguration sourceConfig, CancellationToken ct)
    {
        var fieldMappings = ResolveFieldMappingsForSource(ctx, sourceConfig);
        var filterResult = TranslateFilterForSource<T>(ctx, command, fieldMappings, sourceConfig.SourceName);
        if (filterResult.HasError)
            return filterResult.ErrorResult!;

        var containerName = GetContainerName(sourceConfig);
        if (containerName == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceNoContainer(ctx.Logger, sourceConfig.SourceName, ctx.Config.Name));

        var plan = ResolveAggregation<T>(ctx, command, fieldMappings);
        if (plan.HasError)
            return plan.ErrorResult!;

        var sourceQuery = SourceQuery<T>(command, filterResult.TranslatedFilter, fieldMappings, plan.Aggregation, plan.GroupMappings);
        var result = await ExecuteSourceQuery<T>(ctx, sourceQuery, sourceConfig, ct).ConfigureAwait(false);

        return result.IsFailure
            ? result
            : await ShapeRead(ctx, result, fieldMappings, sourceConfig, plan.Aggregation, plan.GroupMappings, ct).ConfigureAwait(false);
    }

    /// <summary>The query the source is asked: the caller's filter, order and page in the store's names, plus the aggregation.</summary>
    /// <remarks>
    /// Read through IQueryCommand, not QueryCommand&lt;T&gt;: T is the RESULT type (IEnumerable&lt;IDataRow&gt;), not the
    /// command's row type, so a QueryCommand&lt;T&gt; test never matched and the caller's filter, ordering and paging
    /// were silently dropped (FDW-877). Ordering is translated to physical names exactly as the filter is, so a
    /// sort on a mapped field reaches the store under the column it actually has (FDW-876).
    /// A caller's grouping orders by a group column or an aggregate's output column: only the group columns have a
    /// physical name, and an output column is already the name the caller gave it. A data set's own aggregates are
    /// never paged, as before: the translator used to ignore a page on them, and a reader that pages through one
    /// must keep reading all of it.
    /// </remarks>
    private static QueryCommand<T> SourceQuery<T>(
        IDataCommand command,
        IFilterExpression? translatedFilter,
        IReadOnlyDictionary<string, string>? fieldMappings,
        IAggregationExpression? aggregation,
        IReadOnlyDictionary<string, string>? groupMappings)
    {
        var requested = command as IQueryCommand;
        return new QueryCommand<T>
        {
            Filter = translatedFilter,
            Ordering = PredicatePushdownAnalyzer.TranslateOrderingToPhysical(requested?.Ordering, groupMappings is null ? fieldMappings : groupMappings),
            Paging = aggregation is not null && groupMappings is null ? null : requested?.Paging,
            Aggregation = aggregation
        };
    }

    /// <summary>The rows a read answers with: shaped into the data set's rows, or, for a grouped read, with its group columns named as the data set names them.</summary>
    /// <remarks>
    /// An aggregated read returns measures rather than rows, so it is not shaped. A caller's grouping returns its
    /// group columns under the store's names: they are given back as the data set's own, and only those columns are
    /// renamed, so an aggregate's output name is never taken for one.
    /// </remarks>
    private static async Task<IGenericResult<T>> ShapeRead<T>(
        DataSetExecutionContext ctx,
        IGenericResult<T> result,
        IReadOnlyDictionary<string, string>? fieldMappings,
        DataSetSourceConfiguration sourceConfig,
        IAggregationExpression? aggregation,
        IReadOnlyDictionary<string, string>? groupMappings,
        CancellationToken ct)
    {
        if (aggregation is null)
            return await ShapeRows(ctx, result, fieldMappings, sourceConfig, ct).ConfigureAwait(false);

        return groupMappings is { Count: > 0 }
            ? DataSetExecutionHelpers.ApplyFieldRename(ctx, result, groupMappings, sourceConfig.SourceName)
            : result;
    }

    /// <summary>
    /// The aggregation a read runs: the data set's own (built from its definitions), or the one the caller's
    /// query carries, with its field names turned into the store's.
    /// </summary>
    /// <remarks>
    /// Both at once is refused: the data set's result is already grouped, and a second grouping over it
    /// would be a different query from either. <c>GroupMappings</c> is set only for the caller's, and holds
    /// the logical-to-physical names of its group columns.
    /// </remarks>
    private static (bool HasError, IAggregationExpression? Aggregation, IReadOnlyDictionary<string, string>? GroupMappings, IGenericResult<T>? ErrorResult) ResolveAggregation<T>(
        DataSetExecutionContext ctx, IDataCommand command, IReadOnlyDictionary<string, string>? fieldMappings)
    {
        if (ctx.Config is not ISimpleDataSetConfiguration simple)
            return (true, null, null, DataSetExecutionHelpers.KindMismatch<T>(ctx, "Simple"));

        var requested = (command as IQueryCommand)?.Aggregation;
        var defined = simple.Aggregates is { Count: > 0 };

        if (requested is null)
        {
            if (!defined)
                return (false, null, null, null);

            var built = BuildAggregation<T>(ctx, simple.Aggregates);
            return (built.HasError, built.Aggregation, null, built.ErrorResult);
        }

        if (defined)
        {
            return (true, null, null, GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("DataSetNotGroupable"), ctx.Logger,
                ResultDetails.Create("dataSet", ctx.Config.Name, "reason", "it already defines aggregates of its own")));
        }

        var groupMappings = fieldMappings is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : requested.GroupByFields.Where(fieldMappings.ContainsKey)
                .ToDictionary(g => g, g => fieldMappings[g], StringComparer.OrdinalIgnoreCase);
        return (false, ToPhysical(requested, fieldMappings), groupMappings, null);
    }

    private static AggregationExpression ToPhysical(IAggregationExpression requested, IReadOnlyDictionary<string, string>? fieldMappings) => new()
    {
        GroupByFields = [.. requested.GroupByFields.Select(g => fieldMappings is not null && fieldMappings.TryGetValue(g, out var physical) ? physical : g)],
        Aggregations = requested.Aggregations.ToDictionary(a => a.Key, a => PhysicalMeasure(a.Value, fieldMappings), StringComparer.Ordinal),
        IncludeGroupCount = requested.IncludeGroupCount,
    };

    // A measure is "FUNC(field)" or "FUNC(*)": the field inside is the data set's name for it, and the store
    // is asked for its own column.
    private static string PhysicalMeasure(string measure, IReadOnlyDictionary<string, string>? fieldMappings)
    {
        var open = measure.IndexOf('(', StringComparison.Ordinal);
        var close = measure.LastIndexOf(')');
        if (fieldMappings is null || open <= 0 || close <= open)
            return measure;

        var inner = measure.Substring(open + 1, close - open - 1).Trim();
        return fieldMappings.TryGetValue(inner, out var physical) ? $"{measure.Substring(0, open)}({physical})" : measure;
    }

    /// <summary>
    /// Turns physical source rows into the data set's logical rows, in order: rename to logical
    /// fields, run each field's attached transformation chain (FDW-877), then add calculated fields.
    /// An aggregated read returns measures rather than rows, so it is not shaped.
    /// </summary>
    private static async Task<IGenericResult<T>> ShapeRows<T>(
        DataSetExecutionContext ctx,
        IGenericResult<T> result,
        IReadOnlyDictionary<string, string>? fieldMappings,
        DataSetSourceConfiguration sourceConfig,
        CancellationToken ct)
    {
        if (result.IsFailure)
            return result;

        if (fieldMappings != null && fieldMappings.Count > 0 && result.Value != null)
            result = DataSetExecutionHelpers.ApplyFieldRename(ctx, result, fieldMappings, sourceConfig.SourceName);

        var chains = await DataSetExecutionHelpers.ResolveChains(ctx, [sourceConfig], ct).ConfigureAwait(false);
        if (chains.IsFailure)
            return chains.ToNewResult<T>();
        if (chains.Value!.Count > 0 && result.Value != null)
            result = await DataSetExecutionHelpers.ApplyFieldTransformations(ctx, result, chains.Value, sourceConfig.SourceName, ct).ConfigureAwait(false);

        var calculatedFields = ctx.Config.Fields.Where(f => f.IsCalculated).ToList();
        if (calculatedFields.Count > 0 && result.Value != null)
        {
            DataGatewayLogger.ApplyingCalculatedFields(ctx.Logger, calculatedFields.Count, ctx.Config.Name);
            result = DataSetExecutionHelpers.ApplyCalculatedFields(ctx, result, calculatedFields, ctx.Config.Name);
        }

        return result;
    }

    /// <summary>
    /// Builds an <see cref="AggregationExpression"/> from a dataset's aggregate definitions: the union of
    /// their group-by keys plus one <c>FUNC(field)</c> measure per definition. Fails loud (no fallback)
    /// on an empty group-by element, an empty output column, or an aggregate function that cannot be
    /// pushed down to SQL.
    /// </summary>
    private static (bool HasError, IAggregationExpression? Aggregation, IGenericResult<T>? ErrorResult) BuildAggregation<T>(
        DataSetExecutionContext ctx, IEnumerable<DataSetAggregateConfiguration> defs)
    {
        var groupBy = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var measures = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var def in defs.OrderBy(d => d.Ordinal))
        {
            foreach (var raw in def.GroupByFieldNames.Split(','))
            {
                var groupField = raw.Trim();
                if (groupField.Length == 0)
                    return (true, null, GenericResult<T>.Failure(DataGatewayLogger.DataSetValidationFailed(
                        ctx.Logger, ctx.Config.Name, $"Aggregate '{def.AggregateColumnName}' has an empty group-by field.")));
                if (seen.Add(groupField))
                    groupBy.Add(groupField);
            }

            if (string.IsNullOrWhiteSpace(def.AggregateColumnName))
                return (true, null, GenericResult<T>.Failure(DataGatewayLogger.DataSetValidationFailed(
                    ctx.Logger, ctx.Config.Name, "An aggregate definition has an empty output column name.")));

            var sqlFunc = MapToSqlFunction(def.AggregateFunctionName);
            if (sqlFunc is null)
                return (true, null, GenericResult<T>.Failure(DataGatewayLogger.DataSetValidationFailed(
                    ctx.Logger, ctx.Config.Name, $"Aggregate function '{def.AggregateFunctionName}' is not supported for SQL pushdown.")));

            var inner = string.IsNullOrWhiteSpace(def.InputFieldName) ? "*" : def.InputFieldName.Trim();
            measures[def.AggregateColumnName.Trim()] = $"{sqlFunc}({inner})";
        }

        return (false, new AggregationExpression { GroupByFields = groupBy, Aggregations = measures }, null);
    }

    /// <summary>
    /// Maps a <c>DataSetAggregate.AggregateFunctionName</c> to the SQL-pushdownable keyword the query
    /// translator emits, or <see langword="null"/> when the function has no SQL-aggregate form.
    /// </summary>
    private static string? MapToSqlFunction(string functionName)
        => functionName.Trim().ToUpperInvariant() switch
        {
            "SUM" => "SUM",
            "COUNT" => "COUNT",
            "AVG" or "AVERAGE" or "MEAN" => "AVG",
            "MIN" or "MINIMUM" => "MIN",
            "MAX" or "MAXIMUM" => "MAX",
            _ => null
        };

    private static IReadOnlyDictionary<string, string>? ResolveFieldMappingsForSource(DataSetExecutionContext ctx, DataSetSourceConfiguration sourceConfig)
    {
        return sourceConfig.FieldMappings.Count > 0 ? sourceConfig.FieldMappings : null;
    }

    private static (bool HasError, IFilterExpression? TranslatedFilter, IGenericResult<T>? ErrorResult) TranslateFilterForSource<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        IReadOnlyDictionary<string, string>? fieldMappings,
        string sourceName)
    {
        if (command is not IQueryCommand { Filter: not null } queryCommand)
            return (false, null, null);

        var translationResult = ctx.Pushdown.TranslateToPhysical(queryCommand.Filter, fieldMappings);
        if (!translationResult.IsSuccess)
        {
            return (true, null, GenericResult<T>.Failure(
                DataGatewayLogger.FilterTranslationFailed(ctx.Logger, sourceName, translationResult.CurrentMessage ?? "Unknown error")));
        }

        return (false, translationResult.Value, null);
    }

    private static string? GetContainerName(DataSetSourceConfiguration source)
        => DataSetExecutionHelpers.GetContainerName(source);

    private static async Task<IGenericResult<T>> ExecuteSourceQuery<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        DataSetSourceConfiguration sourceConfig,
        CancellationToken ct)
    {
        DataGatewayLogger.ExecuteSourceQueryEntering(ctx.Logger, sourceConfig.SourceName, sourceConfig.DataStoreName);

        var containerName = GetContainerName(sourceConfig);
        if (string.IsNullOrEmpty(containerName))
            return GenericResult<T>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceConfig.SourceName));

        var containerResult = await ctx.DataStoreProvider
            .Get(sourceConfig.DataStoreName, sourceConfig.PathValue, containerName, ct)
            .ConfigureAwait(false);
        if (!containerResult.IsSuccess || containerResult.Value == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceConfig.SourceName));

        var connectionId = containerResult.Value.Parent?.Store?.ConnectionId ?? Guid.Empty;
        if (connectionId == Guid.Empty)
            return GenericResult<T>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConfig.DataStoreName,
                    "the container's DataStore carries no ConnectionId"));

        var connectionResult = await ctx.ConnectionProvider.Get(connectionId, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConfig.DataStoreName, connectionResult.CurrentMessage ?? "Unknown error"));

        return await RunOnConnection<T>(ctx, connectionResult.Value, containerResult.Value, command, sourceConfig, ct).ConfigureAwait(false);
    }

    /// <summary>Sends the command to the source's connection and logs how it went.</summary>
    /// <remarks>
    /// Only a connection that runs a GROUP BY is sent an aggregation: any other would answer with the rows
    /// ungrouped and nothing to say it had not grouped them.
    /// </remarks>
    private static async Task<IGenericResult<T>> RunOnConnection<T>(
        DataSetExecutionContext ctx,
        IDataConnection connection,
        IDataContainer container,
        IDataCommand command,
        DataSetSourceConfiguration sourceConfig,
        CancellationToken ct)
    {
        if (command is IQueryCommand { Aggregation: not null } && !connection.AggregatesInStore)
        {
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("AggregationNotSupportedBySource"), ctx.Logger,
                ResultDetails.Create("dataSet", ctx.Config.Name, "source", sourceConfig.SourceName));
        }

        var result = await connection.Execute<T>(command, container, ct).ConfigureAwait(false);

        if (result.IsSuccess)
            DataGatewayLogger.SourceQueryCompleted(ctx.Logger, sourceConfig.SourceName, 0);
        else
            DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConfig.DataStoreName, result.CurrentMessage ?? "Source query failed");

        return result.IsFailure ? result : GenericResult<T>.Success(result.Value!);
    }

}
