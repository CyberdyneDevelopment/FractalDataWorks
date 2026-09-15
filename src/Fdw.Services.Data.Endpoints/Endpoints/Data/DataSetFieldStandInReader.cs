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

namespace Fdw.Services.Data.Endpoints;

/// <summary>Finds a field's currently active stand-in strategy.</summary>
/// <remarks>
/// data.DataSetFieldStandIn carries no strategy-type column, so which strategy is active is found
/// by checking which of the seven strategy tables has a current, non-deleted row for the stand-in
/// -- exactly one ever does, since every DataSetFieldStandInWriter.Write call mints a fresh parent
/// row and only ever inserts into the one child table its request names.
/// </remarks>
internal static class DataSetFieldStandInReader
{
    public static async Task<IGenericResult<DataSetFieldStandInResponse?>> FindActive(
        IDataGateway gateway, string dataStoreName, string pathName, Guid dataSetFieldId, string fieldName, CancellationToken ct)
    {
        var standInTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetFieldStandIn");
        var standInCommand = new QueryCommand<DataSetFieldStandInConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataSetFieldId", Operator = FilterOperators.ByName("Equal"), Value = dataSetFieldId },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var standInResult = await gateway.Execute<IEnumerable<DataSetFieldStandInConfiguration>>(standInCommand, standInTarget, ct)
            .ConfigureAwait(false);
        if (standInResult.IsFailure) return standInResult.ToNewResult<DataSetFieldStandInResponse?>();

        var standIn = standInResult.Value?.FirstOrDefault();
        if (standIn is null) return GenericResult<DataSetFieldStandInResponse?>.Success(null);

        var strategy = await FindStrategy(gateway, dataStoreName, pathName, standIn.Id, ct).ConfigureAwait(false);
        if (strategy.IsFailure) return strategy.ToNewResult<DataSetFieldStandInResponse?>();

        return GenericResult<DataSetFieldStandInResponse?>.Success(new DataSetFieldStandInResponse
        {
            FieldId = dataSetFieldId,
            Field = fieldName,
            Strategy = strategy.Value!.Value.Name,
            NullPercent = standIn.NullPercent,
            Seed = standIn.Seed,
            Parameters = strategy.Value.Value.Parameters,
        });
    }

    private static async Task<IGenericResult<(string Name, Dictionary<string, object?> Parameters)?>> FindStrategy(
        IDataGateway gateway, string dataStoreName, string pathName, Guid dataSetFieldStandInId, CancellationToken ct)
    {
        Func<Task<IGenericResult<(string, Dictionary<string, object?>)?>>>[] probes =
        [
            () => FindFixedValue(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
            () => FindSequence(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
            () => FindNumericRange(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
            () => FindDateRange(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
            () => FindRegexPattern(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
            () => FindDrawFromDataSet(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
            () => FindWeightedPick(gateway, dataStoreName, pathName, dataSetFieldStandInId, ct),
        ];

        foreach (var probe in probes)
        {
            var result = await probe().ConfigureAwait(false);
            if (result.IsFailure || result.Value is not null) return result;
        }

        return GenericResult<(string, Dictionary<string, object?>)?>.Success(null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindFixedValue(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var result = await Query<FixedValueStandInConfiguration>(gateway, dataStoreName, pathName, "FixedValueStandIn", id, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(result.Value is { } fv
            ? ("FixedValue", new Dictionary<string, object?>(StringComparer.Ordinal) { ["fixedValue"] = fv.FixedValue })
            : null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindSequence(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var result = await Query<SequenceStandInConfiguration>(gateway, dataStoreName, pathName, "SequenceStandIn", id, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(result.Value is { } seq
            ? ("Sequence", new Dictionary<string, object?>(StringComparer.Ordinal) { ["startValue"] = seq.StartValue, ["increment"] = seq.Increment })
            : null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindNumericRange(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var result = await Query<NumericRangeStandInConfiguration>(gateway, dataStoreName, pathName, "NumericRangeStandIn", id, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(result.Value is { } nr
            ? ("NumericRange", new Dictionary<string, object?>(StringComparer.Ordinal) { ["minValue"] = nr.MinValue, ["maxValue"] = nr.MaxValue, ["decimals"] = nr.Decimals })
            : null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindDateRange(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var result = await Query<DateRangeStandInConfiguration>(gateway, dataStoreName, pathName, "DateRangeStandIn", id, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(result.Value is { } dr
            ? ("DateRange", new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["fromDate"] = dr.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["toDate"] = dr.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            })
            : null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindRegexPattern(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var result = await Query<RegexPatternStandInConfiguration>(gateway, dataStoreName, pathName, "RegexPatternStandIn", id, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(result.Value is { } rp
            ? ("RegexPattern", new Dictionary<string, object?>(StringComparer.Ordinal) { ["pattern"] = rp.Pattern })
            : null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindDrawFromDataSet(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var result = await Query<DrawFromDataSetStandInConfiguration>(gateway, dataStoreName, pathName, "DrawFromDataSetStandIn", id, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(result.Value is { } dfds
            ? ("DrawFromDataSet", new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["sourceDataSetId"] = dfds.SourceDataSetId.ToString(),
                ["sourceFieldId"] = dfds.SourceFieldId.ToString(),
            })
            : null);
    }

    private static async Task<IGenericResult<(string, Dictionary<string, object?>)?>> FindWeightedPick(
        IDataGateway gateway, string dataStoreName, string pathName, Guid id, CancellationToken ct)
    {
        var pick = await Query<WeightedPickStandInConfiguration>(gateway, dataStoreName, pathName, "WeightedPickStandIn", id, ct).ConfigureAwait(false);
        if (pick.IsFailure) return pick.ToNewResult<(string, Dictionary<string, object?>)?>();
        if (pick.Value is not { } wp) return GenericResult<(string, Dictionary<string, object?>)?>.Success(null);

        var values = await QueryMany<WeightedPickStandInValueConfiguration>(gateway, dataStoreName, pathName, "WeightedPickStandInValue", "WeightedPickStandInId", wp.Id, ct).ConfigureAwait(false);
        if (values.IsFailure) return values.ToNewResult<(string, Dictionary<string, object?>)?>();
        return GenericResult<(string, Dictionary<string, object?>)?>.Success(("WeightedPick", new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["values"] = (values.Value ?? []).OrderBy(v => v.Ordinal).Select(v => new { v.Value, v.Weight, v.Ordinal }).ToList(),
        }));
    }

    private static Task<IGenericResult<T?>> Query<T>(
        IDataGateway gateway, string dataStoreName, string pathName, string container, Guid dataSetFieldStandInId, CancellationToken ct)
        where T : class
        => Query<T>(gateway, dataStoreName, pathName, container, "DataSetFieldStandInId", dataSetFieldStandInId, ct);

    private static async Task<IGenericResult<T?>> Query<T>(
        IDataGateway gateway, string dataStoreName, string pathName, string container, string filterProperty, Guid filterValue, CancellationToken ct)
        where T : class
    {
        var target = new DataStoreTarget(dataStoreName, pathName, container);
        var command = new QueryCommand<T>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = filterProperty, Operator = FilterOperators.ByName("Equal"), Value = filterValue },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var result = await gateway.Execute<IEnumerable<T>>(command, target, ct).ConfigureAwait(false);
        return result.IsFailure ? result.ToNewResult<T?>() : GenericResult<T?>.Success(result.Value?.FirstOrDefault());
    }

    private static async Task<IGenericResult<IEnumerable<T>?>> QueryMany<T>(
        IDataGateway gateway, string dataStoreName, string pathName, string container, string filterProperty, Guid filterValue, CancellationToken ct)
        where T : class
    {
        var target = new DataStoreTarget(dataStoreName, pathName, container);
        var command = new QueryCommand<T>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = filterProperty, Operator = FilterOperators.ByName("Equal"), Value = filterValue },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        return await gateway.Execute<IEnumerable<T>>(command, target, ct).ConfigureAwait(false);
    }
}
