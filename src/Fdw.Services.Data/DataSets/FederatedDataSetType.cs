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
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using IDataField = Fdw.Data.DataSets.Abstractions.IDataField;

namespace Fdw.Services.Data;

/// <summary>
/// Federated dataset strategy: sources spanning different stores / store types are each pulled
/// independently as <see cref="DataRecord"/> cursors and joined IN MEMORY (hash join), governed by the
/// dataset's federation strategy.
/// </summary>
/// <remarks>
/// Why: registered as the <c>"Federated"</c> member of <see cref="DataSetTypes"/>; selected when a
/// dataset's authored <c>ServiceOptionType</c> is <c>"Federated"</c>. A cross-store join cannot be
/// pushed down to any single backend, so it is performed in the application. The type option is a
/// stateless module-init singleton; per-execution state flows through the
/// <see cref="DataSetExecutionContext"/>.
/// <para>
/// Each source is pulled as <c>IRecordSource&lt;DataRecord&gt;</c> through the
/// <see cref="IRecordSourceConnection"/> capability — the join keys and merges over typed
/// <see cref="DataRecord"/> cells (<c>record["field"]</c>), never <c>dynamic</c>, an
/// <c>ExpandoObject</c>, or a dictionary round-trip. A source whose connection does not advertise the
/// record-source capability FAILS LOUD (NO FALLBACKS) rather than degrading to a materializing path.
/// </para>
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Federated")]
public sealed class FederatedDataSetType : DataSetTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FederatedDataSetType"/> class.</summary>
    public FederatedDataSetType()
        : base(3, "Federated", "Multi-store dataset joined in memory per the federation strategy",
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

        // Why: the helpers take IReadOnlyList; Config.Sources is IList (its concrete List satisfies both),
        // so view it through IReadOnlyList, materializing only if it is some non-IReadOnlyList impl.
        IReadOnlyList<DataSetSourceConfiguration> sources =
            ctx.Config.Sources as IReadOnlyList<DataSetSourceConfiguration> ?? ctx.Config.Sources?.ToList() ?? [];
        if (sources.Count == 0)
            return GenericResult<T>.Failure(DataGatewayLogger.DataSetNoSources(ctx.Logger, ctx.Config.Name));

        // Why: honor the AUTHORED federation strategy — fail loud (no default) when it is missing or
        // not a registered FederationStrategies member. IsParallel drives concurrent vs sequential pulls.
        if (string.IsNullOrWhiteSpace(ctx.Config.FederationStrategy))
            return GenericResult<T>.Failure(
                DataGatewayLogger.FederationStrategyMissing(ctx.Logger, ctx.Config.Name, "no FederationStrategy is configured"));

        var strategy = FederationStrategies.ByName(ctx.Config.FederationStrategy);
        if (ReferenceEquals(strategy, FederationStrategies.NotFound))
            return GenericResult<T>.Failure(
                DataGatewayLogger.FederationStrategyMissing(ctx.Logger, ctx.Config.Name, $"'{ctx.Config.FederationStrategy}' is not a registered FederationStrategies member"));

        DataGatewayLogger.ExecutingMultiSourceDataSet(ctx.Logger, ctx.Config.Name, sources.Count);
        return await ExecuteDistributed<T>(ctx, command, sources, strategy.IsParallel, ct).ConfigureAwait(false);
    }

    private static async Task<IGenericResult<T>> ExecuteDistributed<T>(
        DataSetExecutionContext ctx,
        IDataCommand command,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        bool isParallel,
        CancellationToken ct)
    {
        DataGatewayLogger.ExecutingDistributedDataSetInternal(ctx.Logger, ctx.Config.Name, sources.Count);

        try
        {
            IFilterExpression? inputFilter = command is QueryCommand<T> qc ? qc.Filter : null;
            var fieldMappings = PreResolveFieldMappings(sources);

            var decomposed = ctx.Pushdown.DecomposeBySource(inputFilter, ctx.Config, sources, fieldMappings);
            if (!decomposed.IsSuccess)
                return GenericResult<T>.Failure(
                    DataGatewayLogger.FilterDecompositionFailed(ctx.Logger, ctx.Config.Name, decomposed.CurrentMessage ?? "Unknown error"));

            var sourceFilters = decomposed.Value ?? new Dictionary<string, IFilterExpression>(StringComparer.OrdinalIgnoreCase);
            DataGatewayLogger.FilterDecomposed(ctx.Logger, sourceFilters.Count);

            var pulled = await PullAllSources(ctx, sources, sourceFilters, fieldMappings, isParallel, ct).ConfigureAwait(false);
            if (!pulled.IsSuccess || pulled.Value is null)
                return pulled.ToNewResult<T>();

            var joined = JoinRecords(ctx, pulled.Value, sources);
            if (!joined.IsSuccess || joined.Value is null)
                return joined.ToNewResult<T>();

            DataGatewayLogger.FederatedExecutionCompleted(ctx.Logger, ctx.Config.Name, joined.Value.Count, 0, 0, 0);
            return ConvertRecordsToType<T>(ctx, joined.Value);
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
        // Why: FieldMappings is composed by DataSetConfigurationProvider.Get — no resolver needed.
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
        Dictionary<string, IFilterExpression> sourceFilters,
        Dictionary<string, IReadOnlyDictionary<string, string>> fieldMappings,
        bool isParallel,
        CancellationToken ct)
    {
        async Task<(string SourceName, IGenericResult<MaterializedSource> Result)> RunOne(DataSetSourceConfiguration s)
            => (s.SourceName, await PullSource(ctx, s, sourceFilters, fieldMappings, ct).ConfigureAwait(false));

        (string SourceName, IGenericResult<MaterializedSource> Result)[] results;

        // Why: honor the federation strategy — IsParallel runs the source pulls concurrently; otherwise
        // they run sequentially (e.g. to bound load or preserve order).
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

    private static async Task<IGenericResult<MaterializedSource>> PullSource(
        DataSetExecutionContext ctx,
        DataSetSourceConfiguration sourceConfig,
        Dictionary<string, IFilterExpression> sourceFilters,
        Dictionary<string, IReadOnlyDictionary<string, string>> fieldMappings,
        CancellationToken ct)
    {
        var sourceName = sourceConfig.SourceName;

        // Why: addressing (connection + container) comes exclusively from sourceConfig — commands are
        // address-free shapes (filter/ordering/paging only).
        var connectionName = sourceConfig.ConnectionName;
        if (string.IsNullOrEmpty(connectionName))
            return GenericResult<MaterializedSource>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, "(unset)", "ConnectionName is required for DataSet source queries"));

        IFilterExpression? translatedFilter = null;
        if (sourceFilters.TryGetValue(sourceName, out var rawFilter) && rawFilter is not null)
        {
            fieldMappings.TryGetValue(sourceName, out var mappingsForSource);
            var translation = ctx.Pushdown.TranslateToPhysical(rawFilter, mappingsForSource);
            if (!translation.IsSuccess)
                return GenericResult<MaterializedSource>.Failure(
                    DataGatewayLogger.FilterTranslationFailed(ctx.Logger, sourceName, translation.CurrentMessage ?? "Unknown error"));
            translatedFilter = translation.Value;
        }

        var containerName = GetContainerName(sourceConfig);
        if (string.IsNullOrEmpty(containerName))
            return GenericResult<MaterializedSource>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceName));

        var containerResult = await ctx.DataStoreProvider
            .Get(sourceConfig.DataStoreName, sourceConfig.PathName, containerName, ct)
            .ConfigureAwait(false);
        if (!containerResult.IsSuccess || containerResult.Value is null)
            return GenericResult<MaterializedSource>.Failure(DataGatewayLogger.SourceContainerBuildFailed(ctx.Logger, sourceName));

        var connectionResult = await ctx.ConnectionProvider.Get<IDataConnection>(connectionName, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
            return GenericResult<MaterializedSource>.Failure(
                DataGatewayLogger.ConnectionRetrievalFailed(ctx.Logger, connectionName, connectionResult.CurrentMessage ?? "Unknown error"));

        // Why: a federated in-memory join pulls each source as DataRecord through the record-source
        // capability. A connection that does not advertise it cannot be federated — fail loud (NO
        // FALLBACKS); never degrade to a materializing path (that would silently change the contract).
        if (connectionResult.Value is not IRecordSourceConnection recordConnection)
            return GenericResult<MaterializedSource>.Failure(
                DataGatewayLogger.FederatedSourceNotRecordCapable(ctx.Logger, ctx.Config.Name, sourceName, connectionName));

        DataGatewayLogger.ExecutingSourceQuery(ctx.Logger, sourceName, containerName);
        var sourceQuery = new QueryCommand<DataRecord> { Filter = translatedFilter };
        return await ReadRecords(ctx, recordConnection, sourceQuery, containerResult.Value, sourceName, ct).ConfigureAwait(false);
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
            // Why: each DataRecord owns its own value array (CursorRecordSource allocates per record), so
            // materializing into a list is safe — list entries do not alias one reused buffer.
            var records = new List<DataRecord>();
            await foreach (var recordResult in recordSource.Read(ct).ConfigureAwait(false))
            {
                if (!recordResult.IsSuccess)
                    return GenericResult<MaterializedSource>.Failure(
                        DataGatewayLogger.FederatedRecordReadFailed(ctx.Logger, ctx.Config.Name, sourceName, recordResult.CurrentMessage ?? "Unknown error"));
                records.Add(recordResult.Value);
            }

            DataGatewayLogger.SourceQueryCompleted(ctx.Logger, sourceName, records.Count);
            return GenericResult<MaterializedSource>.Success(new MaterializedSource(records, recordSource.Schema));
        }
    }

    private static IGenericResult<List<DataRecord>> JoinRecords(
        DataSetExecutionContext ctx,
        Dictionary<string, MaterializedSource> sourceData,
        IReadOnlyList<DataSetSourceConfiguration> sources)
    {
        if (ctx.Config.Joins.Count == 0)
        {
            // Why: with no join graph, a federated dataset is the concatenation of every source's records,
            // each keeping its own schema (heterogeneous rows).
            DataGatewayLogger.DataSetNoJoins(ctx.Logger, ctx.Config.Name, sources.Count);
            var all = new List<DataRecord>();
            foreach (var source in sourceData.Values)
                all.AddRange(source.Records);
            return GenericResult<List<DataRecord>>.Success(all);
        }

        List<DataRecord>? accumulated = null;
        RecordSchema? accumulatedSchema = null;

        foreach (var join in ctx.Config.Joins)
        {
            if (!sourceData.TryGetValue(join.LeftSource, out var left))
                return GenericResult<List<DataRecord>>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.LeftSource));
            if (!sourceData.TryGetValue(join.RightSource, out var right))
                return GenericResult<List<DataRecord>>.Failure(
                    DataGatewayLogger.JoinSourceNotFound(ctx.Logger, ctx.Config.Name, join.RightSource));

            // Why: the first join's left side is its left source; each subsequent join chains onto the
            // accumulated result (which already carries the combined schema of the joins so far).
            var leftRecords = accumulated ?? left.Records;
            var leftSchema = accumulatedSchema ?? left.Schema;
            var combinedSchema = CombineSchema(leftSchema, right.Schema);

            accumulated = HashJoin(leftRecords, right.Records, combinedSchema, join.LeftField, join.RightField, join.JoinType);
            accumulatedSchema = combinedSchema;

            DataGatewayLogger.JoinCompleted(
                ctx.Logger, join.LeftSource, join.LeftField, join.RightSource, join.RightField, join.JoinType, accumulated.Count);
        }

        return GenericResult<List<DataRecord>>.Success(accumulated ?? []);
    }

    // Why: hash join over typed DataRecord cells. Keys are read by field NAME through each record's own
    // shared schema (record["field"]); a matched pair merges into one DataRecord over the combined schema.
    private static List<DataRecord> HashJoin(
        List<DataRecord> leftRecords,
        List<DataRecord> rightRecords,
        RecordSchema combinedSchema,
        string leftField,
        string rightField,
        string joinType)
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
                    results.Add(Merge(left, right, combinedSchema));
            }
            else if (isLeftOuter)
            {
                results.Add(MergeLeftOuter(left, combinedSchema));
            }
        }

        return results;
    }

    // Why: a merged row's values are the left record's cells followed by the right record's cells, over
    // the combined schema. No dictionary, no dynamic — straight span copy into one object?[].
    private static DataRecord Merge(DataRecord left, DataRecord right, RecordSchema combinedSchema)
    {
        var values = new object?[combinedSchema.FieldCount];
        var leftValues = left.Values;
        var rightValues = right.Values;
        for (var i = 0; i < leftValues.Length; i++)
            values[i] = leftValues[i];
        for (var i = 0; i < rightValues.Length; i++)
            values[leftValues.Length + i] = rightValues[i];
        return new DataRecord(combinedSchema, values);
    }

    // Why: a left-outer row with no right match carries the left cells and leaves the right portion null.
    private static DataRecord MergeLeftOuter(DataRecord left, RecordSchema combinedSchema)
    {
        var values = new object?[combinedSchema.FieldCount];
        var leftValues = left.Values;
        for (var i = 0; i < leftValues.Length; i++)
            values[i] = leftValues[i];
        return new DataRecord(combinedSchema, values);
    }

    private static RecordSchema CombineSchema(RecordSchema left, RecordSchema right)
        => new(left.Fields.Concat(right.Fields).ToList());

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

        // Why: a DataRecord IS an object row — box each into the object sequence. No dynamic, no dict.
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
                    ResultDetails.Create("TypeName", itemType.Name, "Reason", itemResult.CurrentMessage ?? "Unknown error"));
            list.Add(itemResult.Value!);
        }

        return GenericResult<T>.Success((T)list);
    }

    // Why: a materialized federated source — its records (each owning its value array) plus the shared
    // flyweight schema produced by the source's record cursor. Carried together so the join can build a
    // combined schema and merge cells positionally without re-describing fields per record.
    // Why: pure data holder, no logic beyond trivial construction/assignment
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
