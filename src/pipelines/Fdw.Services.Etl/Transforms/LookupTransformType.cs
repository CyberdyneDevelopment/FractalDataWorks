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
    // Where a run's pre-loaded lookup records live on its ITransformContext.SharedState. This option is a
    // module-init singleton shared by every pipeline in the process, so it cannot hold them itself: a
    // static cache here was emptied by whichever pipeline finished first, under one still running, and the
    // record it was enriching came back without its lookup column (FDW-952). The context belongs to one
    // run, so the cache lives exactly as long as the run does and every page of that run shares it.
    private const string RecordCacheKey = "Fdw.Services.Etl.Transforms.LookupTransformType.RecordCache";

    /// <summary>
    /// Initializes a new instance of the <see cref="LookupTransformType"/> class.
    /// </summary>
    public LookupTransformType() : base(
        name: "Lookup",
        displayName: "Data Lookup",
        description: "Performs lookups against external data sources to enrich records",
        category: "Enrich",
        modifiesStructure: true,
        canFilterRecords: false,
        inputPorts: ["input", "lookup"],
        outputPorts: ["output"])
    {
    }

    private static readonly StageMember[] OwnedMembers =
    [
        new("lookup", "Lookup", true, "The reference data set joined to each record")
    ];

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
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

        var preloaded = await Preload(groups, inputList, context, config.Name, cancellationToken).ConfigureAwait(false);
        if (!preloaded.IsSuccess)
        {
            return preloaded.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
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
            !NamesExactlyOneSource(lookup) ||
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
                Name = column,
                LookupDataSet = lookup.LookupDataSet,
                LookupDataStoreName = lookup.LookupDataStoreName,
                LookupDataStorePath = lookup.LookupDataStorePath,
                LookupContainerName = lookup.LookupContainerName,
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

    // A lookup reads exactly one place (FDW-1269): a data set, or a data store container named by its store, path and container together.
    private static bool NamesExactlyOneSource(ILookupSpec lookup)
    {
        var hasDataSet = !string.IsNullOrWhiteSpace(lookup.LookupDataSet);
        var parts = (string.IsNullOrWhiteSpace(lookup.LookupDataStoreName) ? 0 : 1)
            + (string.IsNullOrWhiteSpace(lookup.LookupDataStorePath) ? 0 : 1)
            + (string.IsNullOrWhiteSpace(lookup.LookupContainerName) ? 0 : 1);
        return hasDataSet ? parts == 0 : parts == 3;
    }

    private static bool ReadsDataStore(PipelineTransformLookupConfiguration lookup) =>
        !string.IsNullOrWhiteSpace(lookup.LookupDataStoreName);

    // What the lookup reads, for its cache key and its log lines: the data set, or store/path/container.
    private static string SourceName(PipelineTransformLookupConfiguration lookup) =>
        ReadsDataStore(lookup)
            ? $"{lookup.LookupDataStoreName}/{lookup.LookupDataStorePath}/{lookup.LookupContainerName}"
            : lookup.LookupDataSet;

    private static string CacheKey(PipelineTransformLookupConfiguration lookup) =>
        $"{SourceName(lookup)}:{lookup.LookupKeyField}:{lookup.SourceKeyField}";

    private static ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>> RecordCacheOf(ITransformContext context)
    {
        // SharedState is a plain Dictionary and every Lookup transform in the run reads it, so the get-or-create
        // is done under a lock on it; the cache it returns is itself a ConcurrentDictionary.
        lock (context.SharedState)
        {
            if (!context.SharedState.TryGetValue(RecordCacheKey, out var existing))
            {
                var created = new ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
                context.SharedState[RecordCacheKey] = created;
                return created;
            }

            return (ConcurrentDictionary<string, ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>>)existing!;
        }
    }

    // A lookup that cannot load its reference data cannot tell a missing key from a key it never looked up, so the
    // stage fails: StageRunner.RunBatch returns the first stage failure and the run stops with the reason.
    private static async Task<IGenericResult> Preload(
        List<IGrouping<string, PipelineTransformLookupConfiguration>> groups,
        List<IDictionary<string, object?>> inputList,
        ITransformContext context,
        string transformName,
        CancellationToken cancellationToken)
    {
        if (context.DataGateway is not IDataGateway gateway)
        {
            return GenericResult.Failure(EtlLog.LookupGatewayMissing(context.Logger, transformName));
        }

        foreach (var group in groups)
        {
            var preloaded = await PreloadGroup(gateway, group.Key, group.First(), inputList, context, transformName, cancellationToken).ConfigureAwait(false);
            if (!preloaded.IsSuccess)
            {
                return preloaded;
            }
        }

        return GenericResult.Success();
    }

    private static async Task<IGenericResult> PreloadGroup(
        IDataGateway gateway,
        string cacheKey,
        PipelineTransformLookupConfiguration sample,
        List<IDictionary<string, object?>> inputList,
        ITransformContext context,
        string transformName,
        CancellationToken cancellationToken)
    {
        var recordCache = RecordCacheOf(context).GetOrAdd(cacheKey, _ => new ConcurrentDictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase));

        var keysToLookup = inputList
            .Select(r => r.TryGetValue(sample.SourceKeyField, out var v) ? v?.ToString() : null)
            .Where(k => k != null && !recordCache.ContainsKey(k!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keysToLookup.Count == 0)
        {
            return GenericResult.Success();
        }

        var batchResult = await PerformBatchLookup(gateway, sample, keysToLookup!, cancellationToken).ConfigureAwait(false);
        if (!batchResult.IsSuccess)
        {
            return GenericResult.Failure(EtlLog.LookupPreloadFailed(
                context.Logger, transformName, SourceName(sample),
                string.Join("; ", batchResult.Messages.Select(message => message.Message))));
        }

        foreach (var kvp in batchResult.Value!)
        {
            recordCache.TryAdd(kvp.Key, kvp.Value);
        }

        EtlLog.LookupBatchPreloaded(context.Logger, transformName, batchResult.Value!.Count, SourceName(sample));
        return GenericResult.Success();
    }

    private static async Task<IGenericResult<Dictionary<string, IReadOnlyDictionary<string, object?>>>> PerformBatchLookup(
        IDataGateway gateway,
        PipelineTransformLookupConfiguration lookup,
        List<string> keyValues,
        CancellationToken cancellationToken)
    {
        try
        {
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

            var result = ReadsDataStore(lookup)
                ? await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
                    queryCommand,
                    new DataStoreTarget(lookup.LookupDataStoreName!, lookup.LookupDataStorePath, lookup.LookupContainerName!),
                    cancellationToken).ConfigureAwait(false)
                : await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
                    queryCommand, new DataSetTarget(lookup.LookupDataSet), cancellationToken).ConfigureAwait(false);

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
    /// (shared connection/dataset/keys), reading matched records from the run's pre-loaded record cache on <c>ITransformContext.SharedState</c>.
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
        var found = RecordCacheOf(context).TryGetValue(cacheKey, out var recordCache) &&
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
