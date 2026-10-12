using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Deletes a data view: <c>DELETE /data-views/{Name}</c> (FDW-1032).</summary>
/// <remarks>
/// Only the user who saved a view may delete it, and only while no other view is built on it: a view of a view reads
/// through its parent, so deleting the parent would leave the child with nothing to read. The refusal names the first
/// view in the way. A workspace that attached the view keeps the attachment, which then reads as left out (the read
/// service logs it), the way an attachment to any retired resource does.
/// </remarks>
public abstract class DeleteDataViewEndpointBase : CrudDeleteEndpointBase<DataViewNameRequest>
{
    private readonly IDataSetVariationConfigurationProvider _variations;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <inheritdoc />
    protected DeleteDataViewEndpointBase(
        ILogger<DeleteDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IAuthenticationContextAccessor authContext) : base(logger)
    {
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "data-views";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DataViewNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(DataViewNameRequest request, CancellationToken ct)
    {
        var found = await DataViewStore.Find(_variations, request.Name, ct).ConfigureAwait(false);
        return found.IsFailure
            ? found.ToNewResult<bool>()
            : GenericResult<bool>.Success(found.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(DataViewNameRequest request, CancellationToken ct)
    {
        var found = await DataViewStore.Find(_variations, request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found;

        // The base already established existence, so a null here is an inconsistency, not a 404.
        if (found.Value is not { } view)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var owned = DataViewStore.OwnedByCaller(_authContext, view, Logger);
        if (owned.IsFailure) return owned;

        var dependent = await DataViewStore.FirstDependent(_variations, view.Id, ct).ConfigureAwait(false);
        if (dependent.IsFailure) return dependent;
        if (dependent.Value is { } child)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("DataViewHasDependents"), Logger,
                ResultDetails.Create("name", view.Name, "reason", $"the view '{child}' is built on it"));
        }

        return await _variations.Delete(view.Id, ct).ConfigureAwait(false);
    }
}
