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
/// Generic base endpoint for retrieving fields for a specific data set.
/// Delegates reads to DataSetConfigurationProvider (which assembles field hierarchy on Get).
/// </summary>
public abstract class GetDataSetFieldsEndpointBase : CrudGetEndpointBase<DataSetNameRequest, List<DataSetFieldPayload>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected GetDataSetFieldsEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>
    /// Routes at <c>/datasets/{Name}/fields</c>. The default CRUD route is <c>/datasets/{Name}</c>,
    /// which collides with the GetDataSet endpoint and shadows this one. The fields sub-resource
    /// segment makes the route non-colliding and matches the calculation client's call.
    /// </summary>
    protected override string Route => $"/{ResourceName}/{{Name}}/fields";

    /// <summary>Returns the data set name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <summary>Finds a data set by name and returns its fields.</summary>
    protected override Task<IGenericResult<List<DataSetFieldPayload>?>> FindByIdentifier(DataSetNameRequest request, CancellationToken ct)
    {
        return LoadDataSetFields(request.Name, ct);
    }

    /// <summary>
    /// Loads data set fields via the provider (assembles hierarchy in Get(name)) and returns as field DTOs.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<List<DataSetFieldPayload>?>> LoadDataSetFields(string dataSetName, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingFields(Logger, dataSetName);

        var result = await _dataSetProvider.Get(dataSetName, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<DataSetFieldPayload>?>();

        if (result.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, dataSetName);
            return GenericResult<List<DataSetFieldPayload>?>.Success((List<DataSetFieldPayload>?)null);
        }

        var fields = result.Value.Fields
            .OrderBy(f => f.Ordinal)
            .Select(DataSetQueryHelper.MapToFieldDto)
            .ToList();

        return GenericResult<List<DataSetFieldPayload>?>.Success((List<DataSetFieldPayload>?)fields);
    }
}
