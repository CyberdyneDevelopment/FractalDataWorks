using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for retrieving a specific data set by name.
/// Delegates all reads to DataSetConfigurationProvider.
/// </summary>
public abstract class GetDataSetEndpointBase : CrudGetEndpointBase<DataSetNameRequest, DataSetDetailResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected GetDataSetEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <summary>Finds a data set by name and maps it to a detail DTO.</summary>
    protected override Task<IGenericResult<DataSetDetailResponse?>> FindByIdentifier(DataSetNameRequest request, CancellationToken ct)
    {
        return LoadDataSetDetail(request.Name, ct);
    }

    /// <summary>
    /// Loads a data set by name from the provider and returns as detail DTO.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<DataSetDetailResponse?>> LoadDataSetDetail(string name, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, name, string.Empty);

        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<DataSetDetailResponse?>();

        if (result.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, name);
            return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)null);
        }

        return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)DataSetQueryHelper.MapToDetail(result.Value));
    }
}
