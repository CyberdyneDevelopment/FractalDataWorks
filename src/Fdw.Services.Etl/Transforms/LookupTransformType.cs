using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that enriches records via lookups against external data sources. Reads the typed
/// <see cref="PipelineTransformConfiguration.Lookups"/> cascade children — one row per brought-across
/// output column, sharing connection/keys within a transform — grouped by connection+dataset+key so a
/// single <see cref="IDataGateway"/> query pre-loads every distinct key's full lookup record before any
/// record is enriched.
/// </summary>
[TypeOption(typeof(OptionTransformTypes), "Lookup")]
public sealed class LookupTransformType : TransformTypeBase
{
    // Cache of pre-loaded full lookup records, keyed by "{Connection}:{DataSet}:{KeyField}:{SourceKeyField}"
    // then by the string-formatted source key value. Static + process-wide, cleared by ClearCache() in
    // BatchCopyPipeline's finally block after each pipeline execution.
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>> RecordCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupTransformType"/> class.
    /// </summary>
    public LookupTransformType() : base(
        id: 5,
        name: "Lookup",
        displayName: "Data Lookup",
        description: "Performs lookups against external data sources to enrich records",
        category: "Enrich",
        modifiesStructure: true,
        canFilterRecords: false)
    {
    }

    /// <summary>
    /// Clears all cached lookup data. Call this after pipeline execution completes
    /// to prevent unbounded memory growth across multiple pipeline runs.
    /// </summary>
    public static void ClearCache() => RecordCache.Clear();

    /// <inheritdoc />
    // Why: a single-record lookup resolves only against the cache TransformBatch pre-populates via the
    // real (awaited) DataGateway query — per-record resolution is pure CPU with no I/O of its own.
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Lookup", configuration.GetType().Name)));
        }

        if (config.Lookups.Count == 0)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.LookupParamsMissing(context.Logger, config.Name)));
        }

        var output = new Dictionary<string, object?>(input, StringComparer.OrdinalIgnoreCase);
        foreach (var group in config.Lookups.GroupBy(CacheKey, StringComparer.Ordinal))
        {
            EnrichFromCache(output, group.Key, group, context, input);
        }

        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(output));
    }

    /// <inheritdoc />
    [ConventionOverride(MaxCyclomaticComplexity = 18)] // FDW013: explicit structural validation + per-group preload + per-record enrichment branches
    public override async Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Lookup", configuration.GetType().Name));
        }

        if (config.Lookups.Count == 0)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.LookupParamsMissing(context.Logger, config.Name));
        }

        foreach (var lookup in config.Lookups)
        {
            if (LookupJoinTypes.ByName(lookup.JoinType) == LookupJoinTypes.NotFound)
            {
                return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                    EtlLog.UnknownJoinType(context.Logger, lookup.JoinType, config.Name));
            }
        }

        var inputList = inputs.ToList();
        if (inputList.Count == 0)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(inputList);
        }

        var groups = config.Lookups.GroupBy(CacheKey, StringComparer.Ordinal).ToList();

        if (context.DataGateway is IDataGateway dataGateway)
        {
            foreach (var group in groups)
            {
                await PreloadGroup(dataGateway, group.Key, group.First(), inputList, context, config.Name, cancellationToken).ConfigureAwait(false);
            }
        }

        var results = new List<IDictionary<string, object?>>();
        foreach (var input in inputList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var output = new Dictionary<string, object?>(input, StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                EnrichFromCache(output, group.Key, group, context, input);
            }
            results.Add(output);
        }

        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results);
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        var lookup = spec.Lookup;
        if (lookup == null ||
            string.IsNullOrWhiteSpace(lookup.LookupConnectionName) ||
            string.IsNullOrWhiteSpace(lookup.LookupDataSet) ||
            string.IsNullOrWhiteSpace(lookup.LookupKeyField) ||
            string.IsNullOrWhiteSpace(lookup.SourceKeyField) ||
            lookup.LookupColumns.Count == 0)
        {
            return GenericResult.Failure(EtlLog.LookupParamsMissing(logger, spec.Name));
        }

        if (LookupJoinTypes.ByName(lookup.JoinType) == LookupJoinTypes.NotFound)
        {
            return GenericResult.Failure(EtlLog.UnknownJoinType(logger, lookup.JoinType, spec.Name));
        }

        config.Lookups = lookup.LookupColumns
            .Select(column => new PipelineTransformLookupConfiguration
            {
                PipelineTransformId = config.Id,
                Name = column,
                LookupConnectionName = lookup.LookupConnectionName,
                LookupDataSet = lookup.LookupDataSet,
                LookupKeyField = lookup.LookupKeyField,
                SourceKeyField = lookup.SourceKeyField,
                OutputFieldPrefix = lookup.OutputFieldPrefix,
                LookupValueField = column,
                JoinType = lookup.JoinType
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static string CacheKey(PipelineTransformLookupConfiguration lookup) =>
        $"{lookup.LookupConnectionName}:{lookup.LookupDataSet}:{lookup.LookupKeyField}:{lookup.SourceKeyField}";

    private static async Task PreloadGroup(
        IDataGateway dataGateway,
        string cacheKey,
        PipelineTransformLookupConfiguration sample,
        List<IDictionary<string, object?>> inputList,
        ITransformContext context,
        string transformName,
        CancellationToken cancellationToken)
    {
        var recordCache = RecordCache.GetOrAdd(cacheKey, _ => new ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase));

        var keysToLookup = inputList
            .Select(r => r.TryGetValue(sample.SourceKeyField, out var v) ? v?.ToString() : null)
            .Where(k => k != null && !recordCache.ContainsKey(k!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keysToLookup.Count == 0)
        {
            return;
        }

        // Why: await the real DataGateway batch query directly — no sync-over-async, cancellation flows
        // through, and the resolved records seed the shared cache for every column brought across.
        var batchResult = await PerformBatchLookup(dataGateway, sample, keysToLookup!, cancellationToken).ConfigureAwait(false);
        if (batchResult.IsSuccess && batchResult.Value != null)
        {
            foreach (var kvp in batchResult.Value)
            {
                recordCache.TryAdd(kvp.Key, kvp.Value);
            }

            EtlLog.LookupBatchPreloaded(context.Logger, transformName, batchResult.Value.Count, sample.LookupConnectionName, sample.LookupDataSet);
        }
        else if (!batchResult.IsSuccess)
        {
            context.ReportError(batchResult.CurrentMessage ?? "Batch lookup failed", null);
        }
    }

    private static async Task<IGenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>> PerformBatchLookup(
        IDataGateway dataGateway,
        PipelineTransformLookupConfiguration lookup,
        List<string> keyValues,
        CancellationToken cancellationToken)
    {
        try
        {
            // Why: Addressing moved off IDataCommand onto DataStoreTarget; path is null to search
            // all paths in the store (documented DataStoreTarget behaviour). No Fields restriction —
            // the full matched record is cached so every brought-across column can be read from it.
            var queryCommand = new QueryCommand<Dictionary<string, object?>>
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = lookup.LookupKeyField,
                        Operator = FilterOperators.In,
                        Value = keyValues
                    }
                }
            };

            var result = await dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(
                queryCommand, new DataStoreTarget(lookup.LookupConnectionName, null, lookup.LookupDataSet), cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result.ToNewResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>();
            }

            var lookupResults = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in result.Value!)
            {
                if (record.TryGetValue(lookup.LookupKeyField, out var key) && key != null)
                {
                    lookupResults[key.ToString()!] = record;
                }
            }

            return GenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>.Success(lookupResults);
        }
        catch (Exception ex)
        {
            return GenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>.Failure(
                EtlResultCodes.ByName("BatchLookupOperationFailed"),
                ResultDetails.Create().With("Message", ex.Message));
        }
    }

    /// <summary>
    /// Enriches <paramref name="output"/> with every brought-across column for one lookup group
    /// (shared connection/dataset/keys), reading matched records from the pre-loaded <see cref="RecordCache"/>.
    /// A missing key reports a soft error via <see cref="ITransformContext.ReportError"/> only when the
    /// group's join type is Inner (<c>FailOnMissing</c>); a Left join silently leaves the field unset.
    /// </summary>
    private static void EnrichFromCache(
        Dictionary<string, object?> output,
        string cacheKey,
        IEnumerable<PipelineTransformLookupConfiguration> lookupsInGroup,
        ITransformContext context,
        IDictionary<string, object?> input)
    {
        var lookupList = lookupsInGroup as IReadOnlyList<PipelineTransformLookupConfiguration> ?? lookupsInGroup.ToList();
        var sample = lookupList[0];

        if (!input.TryGetValue(sample.SourceKeyField, out var keyValue) || keyValue == null)
        {
            return;
        }

        var keyString = keyValue.ToString() ?? "";
        IReadOnlyDictionary<string, object?>? matchedRecord = null;
        var found = RecordCache.TryGetValue(cacheKey, out var recordCache) &&
            recordCache.TryGetValue(keyString, out matchedRecord);

        foreach (var lookup in lookupList)
        {
            var outputField = string.IsNullOrEmpty(lookup.OutputFieldPrefix)
                ? lookup.LookupValueField
                : lookup.OutputFieldPrefix + lookup.LookupValueField;

            if (found && matchedRecord!.TryGetValue(lookup.LookupValueField, out var value))
            {
                output[outputField] = value;
            }
            else if (LookupJoinTypes.ByName(lookup.JoinType).FailOnMissing)
            {
                context.ReportError($"Lookup failed: no match found for key '{keyString}'", input);
            }
        }
    }
}
