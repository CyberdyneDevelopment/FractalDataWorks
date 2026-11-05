using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using IDataField = Fdw.Data.DataSets.Abstractions.IDataField;
using System.Collections.Immutable;

namespace Fdw.Services.Data;

/// <summary>
/// Federated dataset strategy: sources spanning different stores / store types are each pulled
/// independently as <see cref="DataRecord"/> cursors and joined IN MEMORY (hash join), governed by the
/// dataset's federation strategy.
/// </summary>
/// <remarks>
/// Why: registered as the <c>"Federated"</c> member of <see cref="DataSetTypes"/>; selected when a
/// dataset's authored <c>Implementation</c> is <c>"Federated"</c>. A cross-store join cannot be
/// pushed down to any single backend, so it is performed in the application. The type option is a
/// stateless module-init singleton; per-execution state flows through the
/// <see cref="DataSetExecutionContext"/>.
/// <para>
/// Each source is pulled as <c>IRecordSource&lt;DataRecord&gt;</c> through the
/// <see cref="IRecordSourceConnection"/> capability — the join keys and merges over typed
/// <see cref="DataRecord"/> cells (<c>record["field"]</c>), never <c>dynamic</c>, an
/// or a dictionary round-trip. A source whose connection does not advertise the
/// record-source capability FAILS LOUD (NO FALLBACKS) rather than degrading to a materializing path.
/// </para>
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Federated")]
public sealed class FederatedDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FederatedDataSetType"/> class.</summary>
    public FederatedDataSetType()
        : base("Federated", "Multi-store dataset joined in memory per the federation strategy",
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

        if (command is IQueryCommand { Aggregation: not null })
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("DataSetNotGroupable"), ctx.Logger,
                ResultDetails.Create("dataSet", ctx.Config.Name, "reason", "a Federated data set builds its rows from more than one read, so it has no one query to group"));

        if (ctx.Config is not IFederatedDataSetConfiguration federated)
            return DataSetExecutionHelpers.KindMismatch<T>(ctx, "Federated");

        IReadOnlyList<DataSetSourceConfiguration> sources =
            ctx.Config.Sources switch
            {
                IReadOnlyList<DataSetSourceConfiguration> list => list,
                { } other => other.ToList(),
                _ => [],
            };
        if (sources.Count == 0)
            return GenericResult<T>.Failure(DataGatewayLogger.DataSetNoSources(ctx.Logger, ctx.Config.Name));

        if (string.IsNullOrWhiteSpace(federated.FederationStrategy))
            return GenericResult<T>.Failure(
                DataGatewayLogger.FederationStrategyMissing(ctx.Logger, ctx.Config.Name, "no FederationStrategy is configured"));

        var strategy = FederationStrategies.ByName(federated.FederationStrategy);
        if (ReferenceEquals(strategy, FederationStrategies.NotFound))
            return GenericResult<T>.Failure(
                DataGatewayLogger.FederationStrategyMissing(ctx.Logger, ctx.Config.Name, $"'{federated.FederationStrategy}' is not a registered FederationStrategies member"));

        DataGatewayLogger.ExecutingMultiSourceDataSet(ctx.Logger, ctx.Config.Name, sources.Count);
        return await ExecuteDistributed<T>(ctx, command, sources, federated.Joins, strategy.IsParallel, ct).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<T>> ExecuteDistributed<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IList<DataSetJoinConfiguration> joins,
        bool isParallel,
        CancellationToken ct)
    {
        DataGatewayLogger.ExecutingDistributedDataSetInternal(ctx.Logger, ctx.Config.Name, sources.Count);

        try
        {
            // Ordering and paging are NOT applied here: the join is in memory, so they would have to
            // follow it, and PagesInStore stays false until they do (FDW-876, FDW-884).
            IFilterExpression? inputFilter = (command as IQueryCommand)?.Filter;
            var fieldMappings = PreResolveFieldMappings(sources);

            var decomposed = ctx.Pushdown.DecomposeBySource(inputFilter, ctx.Config, sources, fieldMappings);
            if (!decomposed.IsSuccess)
                return GenericResult<T>.Failure(
                    DataGatewayLogger.FilterDecompositionFailed(ctx.Logger, ctx.Config.Name, decomposed.FailureReason().Message));

            IReadOnlyDictionary<string, IFilterExpression> sourceFilters = decomposed.Value is { } decomposedFilters
                ? decomposedFilters
                : ImmutableDictionary<string, IFilterExpression>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
            DataGatewayLogger.FilterDecomposed(ctx.Logger, sourceFilters.Count);

            var pulled = await PullAllSources(ctx, sources, sourceFilters, fieldMappings, isParallel, ct).ConfigureAwait(false);
            if (!pulled.IsSuccess || pulled.Value is null)
                return pulled.ToNewResult<T>();

            var joined = JoinRecords(ctx, pulled.Value, sources, joins);
            if (!joined.IsSuccess || joined.Value is null)
                return joined.ToNewResult<T>();

            // Each source's attached chains run on the joined logical records (FDW-877).
            var chains = await DataSetExecutionHelpers.ResolveChains(ctx, sources, ct).ConfigureAwait(false);
            if (chains.IsFailure)
                return chains.ToNewResult<T>();
            var transformed = await DataSetExecutionHelpers.ApplyFieldTransformations(ctx, joined.Value, chains.Value!, ctx.Config.Name, ct).ConfigureAwait(false);
            if (transformed.IsFailure)
                return transformed.ToNewResult<T>();

            DataGatewayLogger.FederatedExecutionCompleted(ctx.Logger, ctx.Config.Name, transformed.Value!.Count, 0, 0, 0);
            return ConvertRecordsToType<T>(ctx, transformed.Value!);
        }
        catch (Exception ex)
        {
            return GenericResult<T>.Failure(
                DataGatewayLogger.FederatedExecutionException(ctx.Logger, ex, ctx.Config.Name));
        }
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> PreResolveFieldMappings(
        IReadOnlyList<DataSetSourceConfiguration> sources)
    {
        var fieldMappingsBySource = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            if (source.FieldMappings.Count > 0)
                fieldMappingsBySource[source.SourceName] = source.FieldMappings;
        }
        return fieldMappingsBySource;
    }

    private static async Task<IGenericResult<Dictionary<string, MaterializedSource>>> PullAllSources(
        DataSetExecutionContext ctx,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IReadOnlyDictionary<string, IFilterExpression> sourceFilters,
        Dictionary<string, IReadOnlyDictionary<string, string>> fieldMappings,
        bool isParallel,
        CancellationToken ct)
    {
        async Task<(string SourceName, IGenericResult<MaterializedSource> Result)> RunOne(DataSetSourceConfiguration s)
            => (s.SourceName, await PullSource(ctx, s, sourceFilters, fieldMappings, ct).ConfigureAwait(false));

        (string SourceName, IGenericResult<MaterializedSource> Result)[] results;

        if (isParallel)
        {
            results = await Task.WhenAll(sources.Select(RunOne)).ConfigureAwait(false);
        }
        else
        {
            var sequential = new List<(string, IGenericResult<MaterializedSource>)>(sources.Count);
            foreach (var s in sources)
                sequential.Add(await RunOne(s).ConfigureAwait(false));
            results = sequential.ToArray();
        }

        var map = new Dictionary<string, MaterializedSource>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();
        foreach (var r in results)
        {
            if (r.Result.IsSuccess)
                map[r.SourceName] = r.Result.Value;
            else
                failures.Add($"{r.SourceName}: {r.Result.CurrentMessage}");
        }

        if (failures.Count > 0)
            return GenericResult<Dictionary<string, MaterializedSource>>.Failure(
                DataGatewayLogger.SourceQueryFailures(ctx.Logger, ctx.Config.Name, string.Join("; ", failures)));

        DataGatewayLogger.SourceQueriesCompleted(ctx.Logger, results.Length);
        return GenericResult<Dictionary<string, MaterializedSource>>.Success(map);
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static async Task<IGenericResult<MaterializedSource>> PullSource(
        DataSetExecutionContext ctx,
        DataSetSourceConfiguration sourceConfig,
        IReadOnlyDictionary<string, IFilterExpression> sourceFilters,
        Dictionary<string, IReadOnlyDictionary<string, string>> fieldMappings,
        CancellationToken ct)
    {
        var sourceName = sourceConfig.SourceName;
        fieldMappings.TryGetValue(sourceName, out var mappingsForSource);

        IFilterExpression? translatedFilter = null;
        if (sourceFilters.TryGetValue(sourceName, out var rawFilter) && rawFilter is not null)
        {
            var translation = ctx.Pushdown.TranslateToPhysical(rawFilter, mappingsForSource);
            if (!translation.IsSuccess)
                return GenericResult<MaterializedSource>.Failure(
                    DataGatewayLogger.FilterTranslationFailed(ctx.Logger, sourceName, translation.FailureReason().Message));
            translatedFilter = translation.Value;
        }

        var containerName = GetContainerName(sourceConfig);
        if (string.IsNullOrEmpty(containerName))
            return GenericResult<MaterializedSource>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceName));

        var containerResult = await ctx.DataStoreProvider
            .Get(sourceConfig.DataStoreName, sourceConfig.PathValue, containerName, ct)
            .ConfigureAwait(false);
        if (!containerResult.IsSuccess || containerResult.Value is null)
            return GenericResult<MaterializedSource>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceName));

        if (containerResult.Value.Parent?.Store?.ConnectionId is not { } connectionId || connectionId == Guid.Empty)
            return GenericResult<MaterializedSource>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConfig.DataStoreName,
                    "the container's DataStore carries no ConnectionId"));

        var connectionResult = await ctx.ConnectionProvider.Get(connectionId, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return GenericResult<MaterializedSource>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, sourceConfig.DataStoreName, connectionResult.NoValueReason().Message));

        if (connectionResult.Value is not IRecordSourceConnection recordConnection)
            return GenericResult<MaterializedSource>.Failure(
                DataGatewayLogger.FederatedSourceNotRecordCapable(ctx.Logger, ctx.Config.Name, sourceName, sourceConfig.DataStoreName));

        DataGatewayLogger.ExecutingSourceQuery(ctx.Logger, sourceName, containerName);
        var sourceQuery = new QueryCommand<DataRecord> { Filter = translatedFilter };
        var read = await ReadRecords(ctx, recordConnection, sourceQuery, containerResult.Value, sourceName, ct).ConfigureAwait(false);
        if (read.IsFailure || mappingsForSource is null)
            return read;

        // The records come back in the store's names; the join, the chains and the result use the data set's (FDW-3629).
        var logical = DataSetExecutionHelpers.RenameToLogical(ctx, read.Value.Schema, read.Value.Records, mappingsForSource, sourceName);
        return GenericResult<MaterializedSource>.Success(new MaterializedSource(logical.Records, logical.Schema));
    }

    private static async Task<IGenericResult<MaterializedSource>> ReadRecords(
        DataSetExecutionContext ctx,
        IRecordSourceConnection connection,
        IDataCommand command,
        IDataContainer container,
        string sourceName,
        CancellationToken ct)
    {
        var openResult = await connection.OpenRecordSource(command, container, ct).ConfigureAwait(false);
        if (!openResult.IsSuccess || openResult.Value is null)
            return openResult.ToNewResult<MaterializedSource>();

        var recordSource = openResult.Value;
        await using (recordSource.ConfigureAwait(false))
        {
            var records = new List<DataRecord>();
            await foreach (var recordResult in recordSource.Read(ct).ConfigureAwait(false))
            {
                if (!recordResult.IsSuccess)
                    return GenericResult<MaterializedSource>.Failure(
                        DataGatewayLogger.FederatedRecordReadFailed(ctx.Logger, ctx.Config.Name, sourceName, recordResult.FailureReason().Message));
                records.Add(recordResult.Value);
            }

            DataGatewayLogger.SourceQueryCompleted(ctx.Logger, sourceName, records.Count);
            return GenericResult<MaterializedSource>.Success(new MaterializedSource(records, recordSource.Schema));
        }
    }

    private static IGenericResult<List<DataRecord>> JoinRecords(
        DataSetExecutionContext ctx,
        Dictionary<string, MaterializedSource> sourceData,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IList<DataSetJoinConfiguration> joins)
    {
        if (joins.Count == 0)
        {
            DataGatewayLogger.DataSetNoJoins(ctx.Logger, ctx.Config.Name, sources.Count);
            var all = new List<DataRecord>();
            foreach (var source in sourceData.Values)
                all.AddRange(source.Records);
            return GenericResult<List<DataRecord>>.Success(all);
        }

        List<DataRecord>? accumulated = null;
        RecordSchema? accumulatedSchema = null;

        foreach (var join in joins.OrderBy(j => j.Ordinal))
        {
            if (!sourceData.TryGetValue(join.LeftSourceName, out var left))
                return GenericResult<List<DataRecord>>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.LeftSourceName));
            if (!sourceData.TryGetValue(join.RightSourceName, out var right))
                return GenericResult<List<DataRecord>>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.RightSourceName));

            var leftRecords = accumulated is null ? left.Records : accumulated;
            var leftSchema = accumulatedSchema is null ? left.Schema : accumulatedSchema;

            // A join field missing from its side would read as null on every record and match nothing: refused, never an empty result (FDW-3629).
            if (leftSchema.GetFieldOrdinal(join.LeftFieldName) < 0)
                return GenericResult<List<DataRecord>>.Failure(
                    DataGatewayLogger.JoinFieldNotFound(ctx.Logger, ctx.Config.Name, join.LeftSourceName, join.LeftFieldName));
            if (right.Schema.GetFieldOrdinal(join.RightFieldName) < 0)
                return GenericResult<List<DataRecord>>.Failure(
                    DataGatewayLogger.JoinFieldNotFound(ctx.Logger, ctx.Config.Name, join.RightSourceName, join.RightFieldName));

            // Both sides' keys usually carry the same logical name; the key is kept once, from the left, as SQL's JOIN ... USING does, so a
            // left join that matches nothing still reads its own key and not the right side's null (FDW-3629).
            var sharedKey = string.Equals(join.LeftFieldName, join.RightFieldName, StringComparison.OrdinalIgnoreCase)
                ? right.Schema.GetFieldOrdinal(join.RightFieldName)
                : -1;
            var combinedSchema = CombineSchema(leftSchema, right.Schema, sharedKey);

            accumulated = HashJoin(leftRecords, right.Records, combinedSchema, join.LeftFieldName, join.RightFieldName, join.JoinType, sharedKey);
            accumulatedSchema = combinedSchema;

            DataGatewayLogger.JoinCompleted(
                ctx.Logger, join.LeftSourceName, join.LeftFieldName, join.RightSourceName, join.RightFieldName, join.JoinType, accumulated.Count);
        }

        return GenericResult<List<DataRecord>>.Success(accumulated ?? []);
    }

    private static List<DataRecord> HashJoin(
        List<DataRecord> leftRecords,
        List<DataRecord> rightRecords,
        RecordSchema combinedSchema,
        string leftField,
        string rightField,
        string joinType,
        int sharedKey)
    {
        var rightLookup = new Dictionary<object, List<DataRecord>>();
        foreach (var right in rightRecords)
        {
            var key = right[rightField];
            if (key is null) continue;
            if (!rightLookup.TryGetValue(key, out var bucket))
            {
                bucket = [];
                rightLookup[key] = bucket;
            }
            bucket.Add(right);
        }

        var isLeftOuter = joinType.Equals("Left", StringComparison.OrdinalIgnoreCase);
        var results = new List<DataRecord>();
        foreach (var left in leftRecords)
        {
            var key = left[leftField];
            if (key is not null && rightLookup.TryGetValue(key, out var matches))
            {
                foreach (var right in matches)
                    results.Add(Merge(left, right, combinedSchema, sharedKey));
            }
            else if (isLeftOuter)
            {
                results.Add(MergeLeftOuter(left, combinedSchema));
            }
        }

        return results;
    }

    // The left values, then the right values without the shared key, which the left already holds.
    private static DataRecord Merge(DataRecord left, DataRecord right, RecordSchema combinedSchema, int sharedKey)
    {
        var values = new object?[combinedSchema.FieldCount];
        var leftValues = left.Values;
        var rightValues = right.Values;
        for (var i = 0; i < leftValues.Length; i++)
            values[i] = leftValues[i];
        var next = leftValues.Length;
        for (var i = 0; i < rightValues.Length; i++)
        {
            if (i != sharedKey)
                values[next++] = rightValues[i];
        }

        return new DataRecord(combinedSchema, values);
    }

    private static DataRecord MergeLeftOuter(DataRecord left, RecordSchema combinedSchema)
    {
        var values = new object?[combinedSchema.FieldCount];
        var leftValues = left.Values;
        for (var i = 0; i < leftValues.Length; i++)
            values[i] = leftValues[i];
        return new DataRecord(combinedSchema, values);
    }

    private static RecordSchema CombineSchema(RecordSchema left, RecordSchema right, int sharedKey)
        => new(left.Fields.Concat(right.Fields.Where((_, ordinal) => ordinal != sharedKey)).ToList());

    private static string? GetContainerName(DataSetSourceConfiguration source)
    {
        if (!string.IsNullOrEmpty(source.ContainerName))
            return source.ContainerName;
        if (!string.IsNullOrEmpty(source.HttpEndpoint))
            return source.HttpEndpoint;
        if (!string.IsNullOrEmpty(source.FilePath))
            return source.FilePath;
        return null;
    }

    private static IGenericResult<T> ConvertRecordsToType<T>(DataSetExecutionContext ctx, List<DataRecord> records)
    {
        var targetType = typeof(T);
        if (!targetType.IsGenericType)
            return GenericResult<T>.Failure(DataGatewayLogger.CalculatedResultConversionFailed(ctx.Logger, targetType.Name));

        var genericDef = targetType.GetGenericTypeDefinition();
        if (genericDef != typeof(IEnumerable<>) && genericDef != typeof(List<>) && genericDef != typeof(ICollection<>))
            return GenericResult<T>.Failure(DataGatewayLogger.CalculatedResultConversionFailed(ctx.Logger, targetType.Name));

        var itemType = targetType.GetGenericArguments()[0];

        if (itemType == typeof(DataRecord))
            return GenericResult<T>.Success((T)(object)records);

        if (itemType == typeof(object))
            return GenericResult<T>.Success((T)(object)records.Cast<object>().ToList());

        if (itemType == typeof(Dictionary<string, object?>) || itemType == typeof(IDictionary<string, object?>))
            return GenericResult<T>.Success((T)(object)records
                .Select(r => new Dictionary<string, object?>(r.ToDictionary(), StringComparer.OrdinalIgnoreCase))
                .ToList());

        return ConvertRecordsToPocos<T>(records, itemType);
    }

    private static IGenericResult<T> ConvertRecordsToPocos<T>(List<DataRecord> records, Type itemType)
    {
        var mapper = PocoMapperCollection.ByName(itemType.Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("MapperNotFound"),
                ResultDetails.Create("TypeName", itemType.Name));

        var list = mapper.CreateList();
        foreach (var record in records)
        {
            var itemResult = mapper.MapFromDictionary(
                new Dictionary<string, object?>(record.ToDictionary(), StringComparer.OrdinalIgnoreCase));
            if (!itemResult.IsSuccess)
                return GenericResult<T>.Failure(
                    DataServiceResultCodes.ByName("PocoMappingFailed"),
                    ResultDetails.Create("TypeName", itemType.Name, "Reason", itemResult.FailureReason().Message));
            list.Add(itemResult.Value!);
        }

        return GenericResult<T>.Success((T)list);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private readonly struct MaterializedSource
    {
        public MaterializedSource(List<DataRecord> records, RecordSchema schema)
        {
            Records = records;
            Schema = schema;
        }

        public List<DataRecord> Records { get; }

        public RecordSchema Schema { get; }
    }
}
