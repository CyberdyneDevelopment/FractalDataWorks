using Fdw.Services.Data.Clients.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for retrieving sources for a specific data set.
/// Uses DataSetConfigurationProvider for the dataset lookup; sources are part of the composed aggregate.
/// </summary>
public abstract class GetDataSetSourcesEndpointBase : CrudGetEndpointBase<DataSetNameRequest, List<DataSetSourcePayload>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected GetDataSetSourcesEndpointBase(ILogger<GetDataSetSourcesEndpointBase> logger, DataSetConfigurationProvider dataSetProvider) : base(logger)
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
    /// Loads the data set's sources, each with its field mappings. Override for custom behavior.
    /// </summary>
    /// <remarks>
    /// Reads only the sources and what hangs from them; the data set's fields, joins and filters are
    /// not composed (FDW-951). A data set that does not exist fails as not found.
    /// </remarks>
    protected virtual async Task<IGenericResult<List<DataSetSourcePayload>?>> LoadDataSetSources(string dataSetName, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingSources(Logger, dataSetName);

        var read = await _dataSetProvider.Get<DataSetSourceConfiguration>(dataSetName, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (read.IsFailure) return read.ToNewResult<List<DataSetSourcePayload>?>();

        var sources = read.Value!.Select(DataSetQueryHelper.MapToSourceDto).ToList();
        return GenericResult<List<DataSetSourcePayload>?>.Success((List<DataSetSourcePayload>?)sources);
    }
}
