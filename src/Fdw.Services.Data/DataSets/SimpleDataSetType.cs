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
/// fields), and the writable sink path (Insert/BulkInsert/Update/Delete/Truncate forwarded verbatim to
/// the one source's container).
/// </summary>
/// <remarks>
/// Why: registered as the <c>"Simple"</c> member of <see cref="DataSetTypes"/>; selected when a
/// dataset's authored <c>ServiceOptionType</c> is <c>"Simple"</c>. The type option is a module-init
/// singleton with a parameterless constructor and no DI — it is stateless and reads everything it needs
/// for one execution from the <see cref="DataSetExecutionContext"/>.
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Simple")]
public sealed class SimpleDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SimpleDataSetType"/> class.</summary>
    public SimpleDataSetType()
        : base(1, "Simple", "Single-source dataset (direct pull; also the writable sink)",
            typeof(object), Array.Empty<IDataField>(), category: "DataSetStrategy")
    {
    }

    /// <inheritdoc />
    public override IDataQuery CreateQuery() => new DataQueryBuilder<object>(Name);

    /// <inheritdoc />
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default)
    {
        if (context is not DataSetExecutionContext ctx)
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetConfigurationRequired"));

        var sources = ctx.Config.Sources;
        if (sources is null || sources.Count == 0)
            return GenericResult<T>.Failure(DataGatewayLogger.DataSetNoSources(ctx.Logger, ctx.Config.Name));

        var isWrite = command is IDataCommandWithInput or DeleteCommand or TruncateCommand;

        // Why: a Simple dataset is exactly one source — for both reads and writes (the sink). Any other
        // count is a misconfiguration; fail loud rather than guess which source to target.
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

        // Why: a write command is forwarded verbatim to the one source's container — the read pipeline
        // (filter translation / field rename / calculated fields) does not apply to writes.
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

        // Why: when the dataset carries aggregate definitions (DataSetConfiguration.Aggregates), the read
        // is a query-time GROUP BY pushed down to the source — not a row pull. Build the aggregation
        // expression up front; a bad definition fails loud rather than silently returning ungrouped rows.
        IAggregationExpression? aggregation = null;
        if (ctx.Config.Aggregates is { Count: > 0 } aggregates)
        {
            var aggBuild = BuildAggregation<T>(ctx, aggregates);
            if (aggBuild.HasError)
                return aggBuild.ErrorResult!;
            aggregation = aggBuild.Aggregation;
        }

        var sourceQuery = new QueryCommand<T>
        {
            Filter = filterResult.TranslatedFilter,
            Ordering = command is QueryCommand<T> qc ? qc.Ordering : null,
            Paging = command is QueryCommand<T> qc2 ? qc2.Paging : null,
            Aggregation = aggregation
        };

        var result = await ExecuteSourceQuery<T>(ctx, sourceQuery, sourceConfig, ct).ConfigureAwait(false);

        // Why: a failed source query has nothing to post-process — surface it immediately (this guard also
        // satisfies FDW013: the failure path for 'result' is handled before any success-only use below).
        if (result.IsFailure)
            return result;

        // Why: field rename + calculated fields are row-shape post-processing that assume the source row
        // shape. An aggregated result has a different shape (group keys + measures), so both are skipped
        // when aggregating — the SQL already produced the final columns.
        if (aggregation is null)
        {
            // Why: physical→logical renames before calculated fields so downstream callers see logical names.
            if (fieldMappings != null && fieldMappings.Count > 0 && result.Value != null)
                result = DataSetExecutionHelpers.ApplyFieldRename(ctx, result, fieldMappings, sourceConfig.SourceName);

            var calculatedFields = ctx.Config.Fields.Where(f => f.IsCalculated).ToList();
            if (calculatedFields.Count > 0 && result.Value != null)
            {
                DataGatewayLogger.ApplyingCalculatedFields(ctx.Logger, calculatedFields.Count, ctx.Config.Name);
                result = DataSetExecutionHelpers.ApplyCalculatedFields(ctx, result, calculatedFields, ctx.Config.Name);
            }
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
        DataSetExecutionContext ctx, IEnumerable<DataSetAggregateDefinition> defs)
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

            // Why: empty InputFieldName means COUNT(*) — the count of rows in the group.
            var inner = string.IsNullOrWhiteSpace(def.InputFieldName) ? "*" : def.InputFieldName.Trim();
            measures[def.AggregateColumnName.Trim()] = $"{sqlFunc}({inner})";
        }

        return (false, new AggregationExpression { GroupByFields = groupBy, Aggregations = measures }, null);
    }

    /// <summary>
    /// Maps a <c>DataSetAggregate.AggregateFunctionName</c> to the SQL-pushdownable keyword the query
    /// translator emits, or <see langword="null"/> when the function has no SQL-aggregate form.
    /// </summary>
    // Why: normalization (domain name → SQL keyword), not runtime dispatch — the name was already
    // validated against the AggregationFunctions TypeCollection at create time. Accepts both the
    // collection's PascalCase names and the equivalent SQL keywords; anything else returns null so the
    // caller fails loud (never defaults to a different function).
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
        // Why: FieldMappings is composed by DataSetConfigurationProvider.Get — no resolver needed.
        return sourceConfig.FieldMappings.Count > 0 ? sourceConfig.FieldMappings : null;
    }

    private static (bool HasError, IFilterExpression? TranslatedFilter, IGenericResult<T>? ErrorResult) TranslateFilterForSource<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        IReadOnlyDictionary<string, string>? fieldMappings,
        string sourceName)
    {
        if (command is not QueryCommand<T> queryCommand || queryCommand.Filter == null)
            return (false, null, null);

        var translationResult = ctx.Pushdown.TranslateToPhysical(queryCommand.Filter, fieldMappings);
        if (!translationResult.IsSuccess)
        {
            return (true, null, GenericResult<T>.Failure(
                DataGatewayLogger.FilterTranslationFailed(ctx.Logger, sourceName, translationResult.CurrentMessage ?? "Unknown error")));
        }

        return (false, translationResult.Value, null);
    }

    // Why: delegates to the shared helper so CompoundDataSetType can use the same resolution
    // without duplicating code. The helper is internal to Fdw.Services.Data.
    private static string? GetContainerName(DataSetSourceConfiguration source)
        => DataSetExecutionHelpers.GetContainerName(source);

    private static async Task<IGenericResult<T>> ExecuteSourceQuery<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        DataSetSourceConfiguration sourceConfig,
        CancellationToken ct)
    {
        // Why: addressing (connection + container) comes exclusively from sourceConfig — commands are
        // address-free shapes (filter/ordering/paging only).
        var sourceConnectionName = sourceConfig.ConnectionName;
        if (string.IsNullOrEmpty(sourceConnectionName))
            return GenericResult<T>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, "(unset)", "ConnectionName is required for DataSet source queries"));

        DataGatewayLogger.ExecuteSourceQueryEntering(ctx.Logger, sourceConfig.SourceName, sourceConnectionName);

        var containerName = GetContainerName(sourceConfig);
        if (string.IsNullOrEmpty(containerName))
            return GenericResult<T>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceConfig.SourceName));

        var containerResult = await ctx.DataStoreProvider
            .Get(sourceConfig.DataStoreName, sourceConfig.PathValue, containerName, ct)
            .ConfigureAwait(false);
        if (!containerResult.IsSuccess || containerResult.Value == null)
            return GenericResult<T>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceConfig.SourceName));

        var connectionResult = await ctx.ConnectionProvider.Get<IDataConnection>(sourceConnectionName, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value == null)
            return GenericResult<T>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConnectionName, connectionResult.CurrentMessage ?? "Unknown error"));

        var result = await connectionResult.Value.Execute<T>(command, containerResult.Value, ct).ConfigureAwait(false);

        if (result.IsSuccess)
            DataGatewayLogger.SourceQueryCompleted(ctx.Logger, sourceConfig.SourceName, 0);
        else
            DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConnectionName, result.CurrentMessage ?? "Source query failed");

        return result.IsFailure ? result : GenericResult<T>.Success(result.Value!);
    }

}
