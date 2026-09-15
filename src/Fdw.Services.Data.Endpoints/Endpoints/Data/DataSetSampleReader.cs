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

namespace Fdw.Services.Data.Endpoints;

/// <summary>Reads a data set's samples, and the distinct values already loaded for one field.</summary>
internal static class DataSetSampleReader
{
    public static async Task<IGenericResult<List<DataSetSampleResponse>>> ListForDataSet(
        IDataGateway gateway, string dataStoreName, string pathName,
        Guid dataSetImplementationId, IReadOnlyDictionary<Guid, string> fieldNamesById, CancellationToken ct)
    {
        var sampleTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSample");
        var samplesCommand = new QueryCommand<DataSetSampleConfiguration>
        {
            Filter = CurrentFilter("DataSetImplementationId", dataSetImplementationId)
        };
        var samples = await gateway.Execute<IEnumerable<DataSetSampleConfiguration>>(samplesCommand, sampleTarget, ct).ConfigureAwait(false);
        if (samples.IsFailure) return samples.ToNewResult<List<DataSetSampleResponse>>();

        var responses = new List<DataSetSampleResponse>();
        foreach (var sample in samples.Value ?? [])
        {
            var rows = await LoadRows(gateway, dataStoreName, pathName, sample.Id, fieldNamesById, ct).ConfigureAwait(false);
            if (rows.IsFailure) return rows.ToNewResult<List<DataSetSampleResponse>>();

            responses.Add(new DataSetSampleResponse
            {
                Name = sample.Name,
                Description = sample.Description,
                IsDefault = sample.IsDefault,
                Rows = rows.Value!,
            });
        }

        return GenericResult<List<DataSetSampleResponse>>.Success(responses);
    }

    public static async Task<IGenericResult<IReadOnlyList<string>>> DistinctValuesForField(
        IDataGateway gateway, string dataStoreName, string pathName, Guid dataSetFieldId, CancellationToken ct)
    {
        var valueTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSampleValue");
        var valuesCommand = new QueryCommand<DataSetSampleValueConfiguration>
        {
            Filter = CurrentFilter("DataSetFieldId", dataSetFieldId)
        };
        var values = await gateway.Execute<IEnumerable<DataSetSampleValueConfiguration>>(valuesCommand, valueTarget, ct).ConfigureAwait(false);
        if (values.IsFailure) return values.ToNewResult<IReadOnlyList<string>>();

        var distinct = (values.Value ?? [])
            .Select(v => v.Value)
            .Where(v => v is not null)
            .Select(v => v!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return GenericResult<IReadOnlyList<string>>.Success(distinct);
    }

    private static async Task<IGenericResult<List<Dictionary<string, string?>>>> LoadRows(
        IDataGateway gateway, string dataStoreName, string pathName,
        Guid dataSetSampleId, IReadOnlyDictionary<Guid, string> fieldNamesById, CancellationToken ct)
    {
        var rowTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSampleRow");
        var rowsCommand = new QueryCommand<DataSetSampleRowConfiguration>
        {
            Filter = CurrentFilter("DataSetSampleId", dataSetSampleId)
        };
        var rows = await gateway.Execute<IEnumerable<DataSetSampleRowConfiguration>>(rowsCommand, rowTarget, ct).ConfigureAwait(false);
        if (rows.IsFailure) return rows.ToNewResult<List<Dictionary<string, string?>>>();

        var result = new List<Dictionary<string, string?>>();
        foreach (var row in (rows.Value ?? []).OrderBy(r => r.Ordinal))
        {
            var valueTarget = new DataStoreTarget(dataStoreName, pathName, "DataSetSampleValue");
            var valuesCommand = new QueryCommand<DataSetSampleValueConfiguration>
            {
                Filter = CurrentFilter("DataSetSampleRowId", row.Id)
            };
            var values = await gateway.Execute<IEnumerable<DataSetSampleValueConfiguration>>(valuesCommand, valueTarget, ct).ConfigureAwait(false);
            if (values.IsFailure) return values.ToNewResult<List<Dictionary<string, string?>>>();

            var cells = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var value in values.Value ?? [])
            {
                if (fieldNamesById.TryGetValue(value.DataSetFieldId, out var fieldName))
                    cells[fieldName] = value.Value;
            }

            result.Add(cells);
        }

        return GenericResult<List<Dictionary<string, string?>>>.Success(result);
    }

    private static FilterExpression CurrentFilter(string propertyName, Guid value) => new()
    {
        Root = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = propertyName, Operator = FilterOperators.ByName("Equal"), Value = value },
                new FilterCondition { PropertyName = "IsCurrent", Operator = FilterOperators.ByName("Equal"), Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = FilterOperators.ByName("Equal"), Value = false },
            ]
        }
    };
}
