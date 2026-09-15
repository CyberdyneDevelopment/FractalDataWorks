using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Persists a field's stand-in: a fresh parent row, then the matching strategy's own child row.</summary>
/// <remarks>
/// Each set mints a brand new parent Id (not a new version of the prior one) since the strategy
/// itself, not just its parameters, can change. UX_DataSetFieldStandIn_Field_Current enforces one
/// current row per field, but ConfigurationSaveCommand's own version-on-write only retires by
/// matching <c>Id</c> — a fresh Id never matches, so the prior current row has to be retired
/// explicitly first (same shape as DetachDataverseRelationshipEndpointBase's retire), or the insert
/// below violates that index. Every child row always points at its own write's freshly-minted
/// parent Id, so no old child row is ever still reachable through the new parent — no explicit
/// child delete is needed.
/// </remarks>
internal static class DataSetFieldStandInWriter
{
    public static async Task<IGenericResult<DataSetFieldStandInResponse>> Write(
        IDataGateway gateway, string dataStoreName, string pathName,
        Guid dataSetFieldId, string fieldName, SetDataSetFieldStandInRequest request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var standInTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetFieldStandIn");

        var retired = await RetireCurrent(gateway, standInTarget, dataSetFieldId, now, ct).ConfigureAwait(false);
        if (retired.IsFailure) return retired.ToNewResult<DataSetFieldStandInResponse>();

        var standIn = new DataSetFieldStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = $"{fieldName}:{request.Strategy}",
            DataSetFieldId = dataSetFieldId,
            NullPercent = request.NullPercent,
            Seed = request.Seed,
            CreateDate = now,
        };

        var standInResult = await gateway.Execute<int>(
            new ConfigurationSaveCommand<DataSetFieldStandInConfiguration>(standIn), standInTarget, ct).ConfigureAwait(false);
        if (standInResult.IsFailure) return standInResult.ToNewResult<DataSetFieldStandInResponse>();

        var childResult = await WriteChild(gateway, dataStoreName, pathName, standIn, request, now, ct).ConfigureAwait(false);
        if (childResult.IsFailure) return childResult.ToNewResult<DataSetFieldStandInResponse>();

        return GenericResult<DataSetFieldStandInResponse>.Success(new DataSetFieldStandInResponse
        {
            FieldId = dataSetFieldId,
            Field = fieldName,
            Strategy = request.Strategy,
            NullPercent = standIn.NullPercent,
            Seed = standIn.Seed,
            Parameters = childResult.Value!,
        });
    }

    private static Task<IGenericResult<Dictionary<string, object?>>> WriteChild(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct) =>
        request.Strategy switch
        {
            "FixedValue" => WriteFixedValue(gateway, dataStoreName, pathName, standIn, request, now, ct),
            "Sequence" => WriteSequence(gateway, dataStoreName, pathName, standIn, request, now, ct),
            "NumericRange" => WriteNumericRange(gateway, dataStoreName, pathName, standIn, request, now, ct),
            "DateRange" => WriteDateRange(gateway, dataStoreName, pathName, standIn, request, now, ct),
            "RegexPattern" => WriteRegexPattern(gateway, dataStoreName, pathName, standIn, request, now, ct),
            "WeightedPick" => WriteWeightedPick(gateway, dataStoreName, pathName, standIn, request, now, ct),
            "DrawFromDataSet" => WriteDrawFromDataSet(gateway, dataStoreName, pathName, standIn, request, now, ct),
            _ => Task.FromResult(GenericResult<Dictionary<string, object?>>.Success([])),
        };

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteFixedValue(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var row = new FixedValueStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            FixedValue = request.FixedValue!,
            CreateDate = now,
        };
        var saved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<FixedValueStandInConfiguration>(row),
            new DataStoreTarget(dataStoreName, pathName, "FixedValueStandIn"), ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal) { ["fixedValue"] = row.FixedValue });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteSequence(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var row = new SequenceStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            StartValue = request.StartValue!.Value,
            Increment = request.Increment!.Value,
            CreateDate = now,
        };
        var saved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<SequenceStandInConfiguration>(row),
            new DataStoreTarget(dataStoreName, pathName, "SequenceStandIn"), ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["startValue"] = row.StartValue,
                ["increment"] = row.Increment,
            });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteNumericRange(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var row = new NumericRangeStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            MinValue = request.MinValue!.Value,
            MaxValue = request.MaxValue!.Value,
            Decimals = request.Decimals,
            CreateDate = now,
        };
        var saved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<NumericRangeStandInConfiguration>(row),
            new DataStoreTarget(dataStoreName, pathName, "NumericRangeStandIn"), ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["minValue"] = row.MinValue,
                ["maxValue"] = row.MaxValue,
                ["decimals"] = row.Decimals,
            });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteDateRange(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var row = new DateRangeStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            FromDate = request.FromDate!.Value,
            ToDate = request.ToDate!.Value,
            CreateDate = now,
        };
        var saved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<DateRangeStandInConfiguration>(row),
            new DataStoreTarget(dataStoreName, pathName, "DateRangeStandIn"), ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["fromDate"] = row.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["toDate"] = row.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteRegexPattern(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var row = new RegexPatternStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            Pattern = request.Pattern!,
            CreateDate = now,
        };
        var saved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<RegexPatternStandInConfiguration>(row),
            new DataStoreTarget(dataStoreName, pathName, "RegexPatternStandIn"), ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal) { ["pattern"] = row.Pattern });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteWeightedPick(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var pick = new WeightedPickStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            CreateDate = now,
        };
        var pickSaved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<WeightedPickStandInConfiguration>(pick),
            new DataStoreTarget(dataStoreName, pathName, "WeightedPickStandIn"), ct).ConfigureAwait(false);
        if (pickSaved.IsFailure) return pickSaved.ToNewResult<Dictionary<string, object?>>();

        var valueTarget = new DataStoreTarget(dataStoreName, pathName, "WeightedPickStandInValue");
        foreach (var option in request.Values!)
        {
            var valueRow = new WeightedPickStandInValueConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = $"{pick.Name}:{option.Ordinal}",
                WeightedPickStandInId = pick.Id,
                Value = option.Value,
                Weight = option.Weight,
                Ordinal = option.Ordinal,
                CreateDate = now,
            };
            var valueSaved = await gateway.Execute<int>(
                new ConfigurationSaveCommand<WeightedPickStandInValueConfiguration>(valueRow), valueTarget, ct).ConfigureAwait(false);
            if (valueSaved.IsFailure) return valueSaved.ToNewResult<Dictionary<string, object?>>();
        }

        return GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["values"] = request.Values.Select(v => new { v.Value, v.Weight, v.Ordinal }).ToList(),
        });
    }

    private static async Task<IGenericResult<Dictionary<string, object?>>> WriteDrawFromDataSet(
        IDataGateway gateway, string dataStoreName, string pathName,
        DataSetFieldStandInConfiguration standIn, SetDataSetFieldStandInRequest request, DateTimeOffset now, CancellationToken ct)
    {
        var row = new DrawFromDataSetStandInConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = standIn.Name,
            DataSetFieldStandInId = standIn.Id,
            SourceDataSetId = request.SourceDataSetId!.Value,
            SourceFieldId = request.SourceFieldId!.Value,
            CreateDate = now,
        };
        var saved = await gateway.Execute<int>(
            new ConfigurationSaveCommand<DrawFromDataSetStandInConfiguration>(row),
            new DataStoreTarget(dataStoreName, pathName, "DrawFromDataSetStandIn"), ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<Dictionary<string, object?>>()
            : GenericResult<Dictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["sourceDataSetId"] = row.SourceDataSetId.ToString(),
                ["sourceFieldId"] = row.SourceFieldId.ToString(),
            });
    }

    private static async Task<IGenericResult> RetireCurrent(
        IDataGateway gateway, DataStoreTarget standInTarget, Guid dataSetFieldId, DateTimeOffset now, CancellationToken ct)
    {
        var existingCommand = new QueryCommand<DataSetFieldStandInConfiguration>
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

        var existing = await gateway.Execute<IEnumerable<DataSetFieldStandInConfiguration>>(existingCommand, standInTarget, ct).ConfigureAwait(false);
        if (existing.IsFailure) return existing;

        var current = existing.Value?.FirstOrDefault();
        if (current is null) return GenericResult.Success();

        current.IsCurrent = false;
        current.ModifyDate = now;

        var updateCommand = new UpdateCommand<DataSetFieldStandInConfiguration>(current)
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = current.Id }
            }
        };

        var retired = await gateway.Execute<int>(updateCommand, standInTarget, ct).ConfigureAwait(false);
        return retired.IsFailure ? retired : GenericResult.Success();
    }
}
