using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a data store configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete data store configuration type.</typeparam>
public abstract class DeleteDataStoreEndpointBase<TConfig> : CrudDeleteEndpointBase<DataStoreNameRequest>
    where TConfig : DataStoreConfiguration
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected DeleteDataStoreEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name from the delete request.</summary>
    protected override string GetResourceIdentifier(DataStoreNameRequest request) => request.Name;

    /// <summary>Checks if the data store exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(DataStoreNameRequest request, CancellationToken ct)
    {
        var configResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(configResult.IsSuccess && configResult.Value != null);
    }

    /// <summary>Deletes the data store configuration via the DataGateway.</summary>
    protected override async Task<IGenericResult> Delete(DataStoreNameRequest request, CancellationToken ct)
    {
        var configResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        var config = configResult.IsSuccess ? configResult.Value : null;

        if (config == null)
        {
            return GenericResult.Failure(EndpointLogger.ResourceNotFound(Logger, "DataStore", request.Name));
        }

        var deleteResult = await _dataStoreProvider.Delete(config.Id, ct).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        return GenericResult.Success();
    }
}
