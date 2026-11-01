using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Finds the currently active stand-in strategy of one data set field, or of many at once.</summary>
/// <remarks>
/// data.DataSetFieldStandIn carries no strategy-type column, so which strategy is active is found
/// by checking which of the seven strategy tables has a current, non-deleted row for the stand-in
/// -- exactly one ever does, since every DataSetFieldStandInWriter.Write call mints a fresh parent
/// row and only ever inserts into the one child table its request names. One field and many fields
/// take the same path: <see cref="FindActive"/> asks <see cref="FindActiveMany"/> about a single field.
/// </remarks>
internal static class DataSetFieldStandInReader
{
    // Why: a SQL Server command takes at most 2,100 parameters; an In list over field or stand-in ids is cut into
    // pieces well under that, one query per piece.
    private const int MaxIdsPerQuery = 500;

    private sealed record Strategy(Guid StandInId, string Name, Dictionary<string, object?> Parameters);

    public static async Task<IGenericResult<DataSetFieldStandInResponse?>> FindActive(
        IDataGateway gateway, string dataStoreName, string pathName, Guid dataSetFieldId, string fieldName, ILogger logger, CancellationToken ct)
    {
        var many = await FindActiveMany(gateway, dataStoreName, pathName, [(dataSetFieldId, fieldName)], logger, ct).ConfigureAwait(false);
        if (many.IsFailure) return many.ToNewResult<DataSetFieldStandInResponse?>();

        return GenericResult<DataSetFieldStandInResponse?>.Success(
            many.Value!.TryGetValue(dataSetFieldId, out var found) ? found : null);
    }

    /// <summary>Finds the active stand-in of every named field that has one; a field with none is absent from the result.</summary>
    public static async Task<IGenericResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>>> FindActiveMany(
        IDataGateway gateway, string dataStoreName, string pathName, IReadOnlyList<(Guid FieldId, string Name)> fields, ILogger logger, CancellationToken ct)
    {
        var parents = await QueryMany<DataSetFieldStandInConfiguration>(
            gateway, dataStoreName, pathName, "DataSetFieldStandIn", "DataSetFieldId", fields.Select(f => f.FieldId).ToList(), ct).ConfigureAwait(false);
        if (parents.IsFailure) return parents.ToNewResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>>();

        var standIns = new Dictionary<Guid, DataSetFieldStandInConfiguration>();
        foreach (var parent in parents.Value!) standIns.TryAdd(parent.DataSetFieldId, parent);

        var strategies = await FindStrategies(gateway, dataStoreName, pathName, standIns.Values.Select(s => s.Id).ToList(), ct).ConfigureAwait(false);
        if (strategies.IsFailure) return strategies.ToNewResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>>();

        return Assemble(fields, standIns, strategies.Value!, logger);
    }

    private static IGenericResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>> Assemble(
        IReadOnlyList<(Guid FieldId, string Name)> fields,
        Dictionary<Guid, DataSetFieldStandInConfiguration> standIns,
        IReadOnlyDictionary<Guid, Strategy> strategies,
        ILogger logger)
    {
        var responses = new Dictionary<Guid, DataSetFieldStandInResponse>();
        foreach (var (fieldId, name) in fields)
        {
            if (!standIns.TryGetValue(fieldId, out var standIn)) continue;
            if (!strategies.TryGetValue(standIn.Id, out var strategy))
                return GenericResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>>.Failure(
                    StandInEndpointLog.StandInHasNoStrategy(logger, name, standIn.Id));

            responses[fieldId] = new DataSetFieldStandInResponse
            {
                FieldId = fieldId,
                Field = name,
                Strategy = strategy.Name,
                NullPercent = standIn.NullPercent,
                Seed = standIn.Seed,
                Parameters = strategy.Parameters,
            };
        }

        return GenericResult<IReadOnlyDictionary<Guid, DataSetFieldStandInResponse>>.Success(responses);
    }

    private static async Task<IGenericResult<IReadOnlyDictionary<Guid, Strategy>>> FindStrategies(
        IDataGateway gateway, string dataStoreName, string pathName, List<Guid> standInIds, CancellationToken ct)
    {
        Func<Task<IGenericResult<IReadOnlyList<Strategy>>>>[] probes =
        [
            () => Probe<FixedValueStandInConfiguration>(gateway, dataStoreName, pathName, "FixedValueStandIn", standInIds,
                fv => new Strategy(fv.DataSetFieldStandInId, "FixedValue", new Dictionary<string, object?>(StringComparer.Ordinal) { ["fixedValue"] = fv.FixedValue }), ct),
            () => Probe<SequenceStandInConfiguration>(gateway, dataStoreName, pathName, "SequenceStandIn", standInIds,
                seq => new Strategy(seq.DataSetFieldStandInId, "Sequence", new Dictionary<string, object?>(StringComparer.Ordinal) { ["startValue"] = seq.StartValue, ["increment"] = seq.Increment }), ct),
            () => Probe<NumericRangeStandInConfiguration>(gateway, dataStoreName, pathName, "NumericRangeStandIn", standInIds,
                nr => new Strategy(nr.DataSetFieldStandInId, "NumericRange", new Dictionary<string, object?>(StringComparer.Ordinal) { ["minValue"] = nr.MinValue, ["maxValue"] = nr.MaxValue, ["decimals"] = nr.Decimals }), ct),
            () => Probe<DateRangeStandInConfiguration>(gateway, dataStoreName, pathName, "DateRangeStandIn", standInIds, DateRange, ct),
            () => Probe<RegexPatternStandInConfiguration>(gateway, dataStoreName, pathName, "RegexPatternStandIn", standInIds,
                rp => new Strategy(rp.DataSetFieldStandInId, "RegexPattern", new Dictionary<string, object?>(StringComparer.Ordinal) { ["pattern"] = rp.Pattern }), ct),
            () => Probe<DrawFromDataSetStandInConfiguration>(gateway, dataStoreName, pathName, "DrawFromDataSetStandIn", standInIds, DrawFromDataSet, ct),
            () => FindWeightedPicks(gateway, dataStoreName, pathName, standInIds, ct),
        ];

        var found = new Dictionary<Guid, Strategy>();
        foreach (var probe in probes)
        {
            var result = await probe().ConfigureAwait(false);
            if (result.IsFailure) return result.ToNewResult<IReadOnlyDictionary<Guid, Strategy>>();
            foreach (var strategy in result.Value!) found.TryAdd(strategy.StandInId, strategy);
        }

        return GenericResult<IReadOnlyDictionary<Guid, Strategy>>.Success(found);
    }

    private static Strategy DateRange(DateRangeStandInConfiguration dr) => new(dr.DataSetFieldStandInId, "DateRange", new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["fromDate"] = dr.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["toDate"] = dr.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    });

    private static Strategy DrawFromDataSet(DrawFromDataSetStandInConfiguration dfds) => new(dfds.DataSetFieldStandInId, "DrawFromDataSet", new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["sourceDataSetId"] = dfds.SourceDataSetId.ToString(),
        ["sourceFieldId"] = dfds.SourceFieldId.ToString(),
    });

    private static async Task<IGenericResult<IReadOnlyList<Strategy>>> Probe<T>(
        IDataGateway gateway, string dataStoreName, string pathName, string container, List<Guid> standInIds, Func<T, Strategy> map, CancellationToken ct)
        where T : class
    {
        var rows = await QueryMany<T>(gateway, dataStoreName, pathName, container, "DataSetFieldStandInId", standInIds, ct).ConfigureAwait(false);
        if (rows.IsFailure) return rows.ToNewResult<IReadOnlyList<Strategy>>();
        return GenericResult<IReadOnlyList<Strategy>>.Success(rows.Value!.Select(map).ToList());
    }

    private static async Task<IGenericResult<IReadOnlyList<Strategy>>> FindWeightedPicks(
        IDataGateway gateway, string dataStoreName, string pathName, List<Guid> standInIds, CancellationToken ct)
    {
        var picks = await QueryMany<WeightedPickStandInConfiguration>(gateway, dataStoreName, pathName, "WeightedPickStandIn", "DataSetFieldStandInId", standInIds, ct).ConfigureAwait(false);
        if (picks.IsFailure) return picks.ToNewResult<IReadOnlyList<Strategy>>();
        var pickList = picks.Value!.ToList();

        var values = await QueryMany<WeightedPickStandInValueConfiguration>(
            gateway, dataStoreName, pathName, "WeightedPickStandInValue", "WeightedPickStandInId", pickList.Select(p => p.Id).ToList(), ct).ConfigureAwait(false);
        if (values.IsFailure) return values.ToNewResult<IReadOnlyList<Strategy>>();
        var valuesByPick = values.Value!.ToLookup(v => v.WeightedPickStandInId);

        return GenericResult<IReadOnlyList<Strategy>>.Success(pickList.Select(pick => new Strategy(pick.DataSetFieldStandInId, "WeightedPick",
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["values"] = valuesByPick[pick.Id].OrderBy(v => v.Ordinal).Select(v => new { v.Value, v.Weight, v.Ordinal }).ToList(),
            })).ToList());
    }

    private static async Task<IGenericResult<IReadOnlyList<T>>> QueryMany<T>(
        IDataGateway gateway, string dataStoreName, string pathName, string container, string filterProperty, List<Guid> filterValues, CancellationToken ct)
        where T : class
    {
        var target = new DataStoreTarget(dataStoreName, pathName, container);
        var rows = new List<T>();
        foreach (var piece in filterValues.Distinct().Chunk(MaxIdsPerQuery))
        {
            var command = new QueryCommand<T>
            {
                Filter = new FilterExpression
                {
                    Root = new FilterGroup
                    {
                        Operator = LogicalOperator.And,
                        Nodes =
                        [
                            new FilterCondition { PropertyName = filterProperty, Operator = FilterOperators.ByName("In"), Value = piece.Cast<object?>().ToList() },
                            new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                            new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                        ]
                    }
                }
            };

            var result = await gateway.Execute<IEnumerable<T>>(command, target, ct).ConfigureAwait(false);
            if (result.IsFailure) return result.ToNewResult<IReadOnlyList<T>>();
            if (result.Value is not null) rows.AddRange(result.Value);
        }

        return GenericResult<IReadOnlyList<T>>.Success(rows);
    }
}
