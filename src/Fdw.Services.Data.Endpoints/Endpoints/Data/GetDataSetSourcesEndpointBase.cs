using Fdw.Services.Data.Clients.Models;
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
/// Generic base endpoint for retrieving sources for a specific data set.
/// Uses DataSetConfigurationProvider for the dataset lookup; sources are part of the composed aggregate.
/// </summary>
public abstract class GetDataSetSourcesEndpointBase : CrudGetEndpointBase<DataSetNameRequest, List<DataSetSourcePayload>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected GetDataSetSourcesEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <summary>Finds a data set by name and returns its sources.</summary>
    protected override Task<IGenericResult<List<DataSetSourcePayload>?>> FindByIdentifier(DataSetNameRequest request, CancellationToken ct)
    {
        return LoadDataSetSources(request.Name, ct);
    }

    /// <summary>
    /// Loads data set sources from the composed aggregate returned by the provider.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<List<DataSetSourcePayload>?>> LoadDataSetSources(string dataSetName, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingSources(Logger, dataSetName);

        var dsResult = await _dataSetProvider.Get(dataSetName, ct).ConfigureAwait(false);
        if (dsResult.IsFailure) return dsResult.ToNewResult<List<DataSetSourcePayload>?>();

        if (dsResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, dataSetName);
            return GenericResult<List<DataSetSourcePayload>?>.Success((List<DataSetSourcePayload>?)null);
        }

        // Why: Sources are part of the composed aggregate returned by DataSetConfigurationProvider.Get.
        var sources = dsResult.Value.Sources?.Select(DataSetQueryHelper.MapToSourceDto).ToList() ?? [];
        return GenericResult<List<DataSetSourcePayload>?>.Success((List<DataSetSourcePayload>?)sources);
    }
}
