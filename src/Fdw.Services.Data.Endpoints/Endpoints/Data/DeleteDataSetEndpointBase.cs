using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a data set configuration.
/// Delegates all reads and deletes to DataSetConfigurationProvider.
/// </summary>
public abstract class DeleteDataSetEndpointBase : CrudDeleteEndpointBase<DataSetNameRequest>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected DeleteDataSetEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name from the delete request.</summary>
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <summary>Checks if the data set exists.</summary>
    protected override Task<IGenericResult<bool>> CheckExistsForDelete(DataSetNameRequest request, CancellationToken ct)
    {
        return CheckDataSetExists(request.Name, ct);
    }

    /// <summary>Deletes the data set configuration.</summary>
    protected override Task<IGenericResult> Delete(DataSetNameRequest request, CancellationToken ct)
    {
        return DeleteDataSet(request.Name, ct);
    }

    /// <summary>
    /// Checks if a data set exists by querying the provider.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<bool>> CheckDataSetExists(string name, CancellationToken ct)
    {
        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<bool>();

        return GenericResult<bool>.Success(result.Value is not null);
    }

    /// <summary>
    /// Deletes the data set via the provider.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult> DeleteDataSet(string name, CancellationToken ct)
    {
        DataSetEndpointLog.DeletingDataSet(Logger, name);

        // Why: Load the configuration to get its Id — the provider's Delete(Guid) is the correct
        // path; Delete(string name) resolves via Get(name) internally but we log not-found here.
        var loadResult = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (loadResult.IsFailure) return loadResult;

        if (loadResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, name);
            return GenericResult.Success();
        }

        var deleteResult = await _dataSetProvider.Delete(loadResult.Value.Id, ct).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            DataSetEndpointLog.DataSetDeleteFailed(Logger, name, "Delete failed");
            return deleteResult;
        }

        DataSetEndpointLog.DataSetDeleted(Logger, name);
        return GenericResult.Success();
    }
}
