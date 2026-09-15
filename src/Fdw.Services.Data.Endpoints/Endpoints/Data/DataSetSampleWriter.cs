using System;
using System.Collections.Generic;
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

/// <summary>Replaces one named sample's rows for a data set.</summary>
/// <remarks>
/// Retires whatever current sample shares this (DataSetImplementationId, Name) — DataSetSample
/// carries no unique index over that pair, so the "one current sample per name" guarantee is
/// enforced here rather than by the schema, the same explicit-retire shape
/// <see cref="DataSetFieldStandInWriter"/> uses for its own field-level guarantee. A fresh Id chain
/// is then minted for the sample and every row/value below it; the retired sample's own rows and
/// values stay attached to its now-non-current Id and are never queried again.
/// </remarks>
internal static class DataSetSampleWriter
{
    public static async Task<IGenericResult<DataSetSampleResponse>> Upsert(
        IDataGateway gateway, string dataStoreName, string pathName,
        Guid dataSetImplementationId, IReadOnlyDictionary<string, Guid> fieldIdsByName,
        DataSetSampleRequest request, ILogger logger, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var sampleTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSample");

        var retired = await RetireCurrent(gateway, sampleTarget, dataSetImplementationId, request.SampleName, now, ct).ConfigureAwait(false);
        if (retired.IsFailure) return retired.ToNewResult<DataSetSampleResponse>();

        var sample = new DataSetSampleConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = request.SampleName,
            DataSetImplementationId = dataSetImplementationId,
            Description = request.Description,
            IsDefault = request.IsDefault,
            CreateDate = now,
        };
        var sampleResult = await gateway.Execute<int>(
            new ConfigurationSaveCommand<DataSetSampleConfiguration>(sample), sampleTarget, ct).ConfigureAwait(false);
        if (sampleResult.IsFailure) return sampleResult.ToNewResult<DataSetSampleResponse>();

        if (request.IsDefault)
        {
            var clearedOthers = await ClearOtherDefaults(gateway, sampleTarget, dataSetImplementationId, sample.Id, now, ct).ConfigureAwait(false);
            if (clearedOthers.IsFailure) return clearedOthers.ToNewResult<DataSetSampleResponse>();
        }

        var rows = new List<Dictionary<string, string?>>();
        var rowTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSampleRow");
        var valueTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSampleValue");

        for (var ordinal = 0; ordinal < request.Rows.Count; ordinal++)
        {
            var written = await WriteRow(gateway, rowTarget, valueTarget, sample.Id, ordinal, request.Rows[ordinal], fieldIdsByName, request.Name, logger, now, ct)
                .ConfigureAwait(false);
            if (written.IsFailure) return written.ToNewResult<DataSetSampleResponse>();
            rows.Add(written.Value!);
        }

        return GenericResult<DataSetSampleResponse>.Success(new DataSetSampleResponse
        {
            Name = sample.Name,
            Description = sample.Description,
            IsDefault = sample.IsDefault,
            Rows = rows,
        });
    }

    private static async Task<IGenericResult<Dictionary<string, string?>>> WriteRow(
        IDataGateway gateway, DataStoreTarget rowTarget, DataStoreTarget valueTarget,
        Guid dataSetSampleId, int ordinal, Dictionary<string, string?> cells,
        IReadOnlyDictionary<string, Guid> fieldIdsByName, string dataSetName, ILogger logger, DateTimeOffset now, CancellationToken ct)
    {
        var row = new DataSetSampleRowConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = $"row-{ordinal}",
            DataSetSampleId = dataSetSampleId,
            Ordinal = ordinal,
            CreateDate = now,
        };
        var rowResult = await gateway.Execute<int>(
            new ConfigurationSaveCommand<DataSetSampleRowConfiguration>(row), rowTarget, ct).ConfigureAwait(false);
        if (rowResult.IsFailure) return rowResult.ToNewResult<Dictionary<string, string?>>();

        var written = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (fieldName, value) in cells)
        {
            if (!fieldIdsByName.TryGetValue(fieldName, out var fieldId))
            {
                DataSetSampleEndpointLog.SampleUnknownField(logger, dataSetName, fieldName);
                continue;
            }

            var valueRow = new DataSetSampleValueConfiguration
            {
                Id = Guid.CreateVersion7(),
                Name = $"{row.Name}:{fieldName}",
                DataSetSampleRowId = row.Id,
                DataSetFieldId = fieldId,
                Value = value,
                CreateDate = now,
            };
            var valueResult = await gateway.Execute<int>(
                new ConfigurationSaveCommand<DataSetSampleValueConfiguration>(valueRow), valueTarget, ct).ConfigureAwait(false);
            if (valueResult.IsFailure) return valueResult.ToNewResult<Dictionary<string, string?>>();

            written[fieldName] = value;
        }

        return GenericResult<Dictionary<string, string?>>.Success(written);
    }

    private static async Task<IGenericResult> RetireCurrent(
        IDataGateway gateway, DataStoreTarget sampleTarget, Guid dataSetImplementationId, string sampleName, DateTimeOffset now, CancellationToken ct)
    {
        var existingCommand = new QueryCommand<DataSetSampleConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataSetImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataSetImplementationId },
                        new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("Equal"), Value = sampleName },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var existing = await gateway.Execute<IEnumerable<DataSetSampleConfiguration>>(existingCommand, sampleTarget, ct).ConfigureAwait(false);
        if (existing.IsFailure) return existing;

        var current = existing.Value?.FirstOrDefault();
        if (current is null) return GenericResult.Success();

        current.IsCurrent = false;
        current.ModifyDate = now;

        var updateCommand = new UpdateCommand<DataSetSampleConfiguration>(current)
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = current.Id }
            }
        };

        var retired = await gateway.Execute<int>(updateCommand, sampleTarget, ct).ConfigureAwait(false);
        return retired.IsFailure ? retired : GenericResult.Success();
    }

    /// <summary>Clears IsDefault on every other current sample for this data set, so at most one
    /// current sample is ever the default — confirmed live by react-ui-design as a real gap
    /// (happy-path and HappyPath both came back IsDefault=true).</summary>
    private static async Task<IGenericResult> ClearOtherDefaults(
        IDataGateway gateway, DataStoreTarget sampleTarget, Guid dataSetImplementationId, Guid exceptSampleId, DateTimeOffset now, CancellationToken ct)
    {
        var othersCommand = new QueryCommand<DataSetSampleConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "DataSetImplementationId", Operator = FilterOperators.ByName("Equal"), Value = dataSetImplementationId },
                        new FilterCondition { PropertyName = "IsDefault", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                        new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
                    ]
                }
            }
        };

        var others = await gateway.Execute<IEnumerable<DataSetSampleConfiguration>>(othersCommand, sampleTarget, ct).ConfigureAwait(false);
        if (others.IsFailure) return others;

        foreach (var other in others.Value ?? [])
        {
            if (other.Id == exceptSampleId) continue;

            other.IsDefault = false;
            other.ModifyDate = now;

            var updateCommand = new UpdateCommand<DataSetSampleConfiguration>(other)
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = other.Id }
                }
            };

            var cleared = await gateway.Execute<int>(updateCommand, sampleTarget, ct).ConfigureAwait(false);
            if (cleared.IsFailure) return cleared;
        }

        return GenericResult.Success();
    }
}
