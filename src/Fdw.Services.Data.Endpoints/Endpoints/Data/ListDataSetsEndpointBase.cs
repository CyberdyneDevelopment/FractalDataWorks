using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for listing all configured data sets.
/// Delegates all reads to DataSetConfigurationProvider.
/// </summary>
public abstract class ListDataSetsEndpointBase : CrudListEndpoint<DataSetSummaryResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected ListDataSetsEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Loads all data sets and maps them to summary DTOs.</summary>
    protected override Task<IGenericResult<List<DataSetSummaryResponse>>> LoadItems(CancellationToken ct)
    {
        return LoadDataSetSummaries(ct);
    }

    /// <summary>
    /// Loads all data set configurations and returns as summary DTOs.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<List<DataSetSummaryResponse>>> LoadDataSetSummaries(CancellationToken ct)
    {
        DataSetEndpointLog.QueryingDataSets(Logger, string.Empty);

        // Why: Provider.Get() returns all datasets without child hierarchy (by design — see comment
        // in DataSetConfigurationProvider). Source counts come from SourceIds list already populated
        // on the configuration by the provider's internal query.
        var result = await _dataSetProvider.Get(ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<DataSetSummaryResponse>>();

        var summaries = result.Value?
            .Select(c => DataSetQueryHelper.MapToSummary(c, (c.SourceIds ?? []).Count))
            .ToList() ?? [];

        DataSetEndpointLog.DataSetsLoaded(Logger, summaries.Count);
        return GenericResult<List<DataSetSummaryResponse>>.Success(summaries);
    }
}
