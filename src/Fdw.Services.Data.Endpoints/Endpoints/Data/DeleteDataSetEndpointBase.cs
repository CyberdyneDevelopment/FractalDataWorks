using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for deleting a data set configuration.
/// Delegates all reads and deletes to DataSetConfigurationProvider.
/// </summary>
public abstract class DeleteDataSetEndpointBase : CrudDeleteEndpointBase<DataSetNameRequest>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <inheritdoc />
    protected DeleteDataSetEndpointBase(
        ILogger<DeleteDataSetEndpointBase> logger,
        DataSetConfigurationProvider dataSetProvider,
        IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _variations = variations;
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

        var loadResult = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (loadResult.IsFailure) return loadResult;

        if (loadResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, name);
            return GenericResult.Success();
        }

        var unvaried = await RefuseWhileVaried(loadResult.Value, ct).ConfigureAwait(false);
        if (unvaried.IsFailure) return unvaried;

        var deleteResult = await _dataSetProvider.Delete(loadResult.Value.Id, ct).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            DataSetEndpointLog.DataSetDeleteFailed(Logger, name, "Delete failed");
            return deleteResult;
        }

        DataSetEndpointLog.DataSetDeleted(Logger, name);
        return GenericResult.Success();
    }

    /// <summary>Refuses the delete while a workspace owns a variation of the data set: each one reads through to it.</summary>
    /// <remarks>
    /// One query on the variations' domain table, asking for a page of one with the count: the answer needed is
    /// whether any exist and how many to name. A delete retires the data set's rows and removes none, so no foreign
    /// key refuses it, and this is the only guard. The count is read as the caller; a variation row-level security
    /// hides from them does not block the delete.
    /// </remarks>
    private async Task<IGenericResult> RefuseWhileVaried(IDataSetImplementationConfiguration dataSet, CancellationToken ct)
    {
        var variations = await _variations.Get(
            new DomainQuery(
                new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = nameof(DataSetVariationDomainConfiguration.BaseDataSetId),
                        Operator = FilterOperators.ByName("Equal"),
                        Value = dataSet.Id,
                    },
                },
                null, 0, 1, true),
            ct).ConfigureAwait(false);
        if (variations.IsFailure) return variations;

        return variations.Value!.Total is > 0 and var count
            ? GenericResult.Failure(
                DataSetsResultCodes.ByName("DataSetHasVariations"), Logger,
                ResultDetails.Create("dataSet", dataSet.Name, "count", count.ToString(CultureInfo.InvariantCulture)))
            : GenericResult.Success();
    }
}
