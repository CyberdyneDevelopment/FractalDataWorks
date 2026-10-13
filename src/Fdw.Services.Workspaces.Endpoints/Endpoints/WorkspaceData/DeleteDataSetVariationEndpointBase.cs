using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Deletes a variation a workspace owns: <c>DELETE /workspaces/{WorkspaceName}/data-sets/{Name}</c>.
/// </summary>
/// <remarks>
/// <para>
/// The mirror of <see cref="CreateDataSetVariationEndpointBase"/>: the variation is found by the workspace and the name it
/// is known by there (<see cref="DataSetVariationQueries.Named"/>), so a data view, which a workspace does not own, is
/// never reached by this route (it has <c>DELETE /data-views/{Name}</c>).
/// </para>
/// <para>
/// The caller needs to write the workspace, as when detaching a resource. A caller refused that answers what the policy
/// answers, the same whether or not the variation exists, so the route never confirms a name the caller may not touch.
/// The variation is refused while another view is built on it: a view of a view reads through its parent, so deleting
/// the parent would leave the child with nothing to read. The refusal's details name the first view in the way (they are
/// logged); over HTTP the body carries the result code <c>Workspaces-41000</c> with the mapper's generic conflict message,
/// as every refusal does, so the response does not name it.
/// </para>
/// </remarks>
public abstract class DeleteDataSetVariationEndpointBase : CrudDeleteEndpointBase<DeleteDataSetVariationRequest>
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <summary>Initializes a new instance of the <see cref="DeleteDataSetVariationEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspaces">Resolves the workspace that owns the variation.</param>
    /// <param name="access">Decides whether the caller may write the workspace.</param>
    /// <param name="variations">Finds and deletes the variation.</param>
    protected DeleteDataSetVariationEndpointBase(
        ILogger<DeleteDataSetVariationEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
    }

    /// <inheritdoc />
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>The access policy decides, as it does for creating one, so the platform permission is not the gate (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{WorkspaceName}/data-sets/{Name}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Delete a variation a workspace owns";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Deletes a data set the workspace owns as a variation of a main data set. Needs write access to the workspace. " +
        "Refused with 409 while another view is built on it.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DeleteDataSetVariationRequest request) => request.Name;

    /// <inheritdoc />
    /// <remarks>
    /// A caller the workspace refuses is told "found" so that <see cref="Delete"/> answers with the policy's own refusal: an
    /// absent variation and a refused caller must not look different.
    /// </remarks>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(DeleteDataSetVariationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.WorkspaceName))
            return GenericResult<bool>.Success(false);

        var workspace = await _workspaces.Get(request.WorkspaceName, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<bool>();
        if (workspace.Value is null) return GenericResult<bool>.Success(false);

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(permitted)) return permitted.ToNewResult<bool>();
        if (permitted.IsFailure) return GenericResult<bool>.Success(true);

        var variation = await DataSetVariationQueries.Named(_variations, workspace.Value.Id, request.Name, ct).ConfigureAwait(false);
        return variation.IsFailure
            ? variation.ToNewResult<bool>()
            : GenericResult<bool>.Success(variation.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(DeleteDataSetVariationRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.WorkspaceName, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace;
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<bool>(request.WorkspaceName, Logger);

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted;

        var variation = await DataSetVariationQueries.Named(_variations, workspace.Value.Id, request.Name, ct).ConfigureAwait(false);
        if (variation.IsFailure) return variation;

        // The base already established existence, so a null here is an inconsistency, not a 404.
        if (variation.Value is not { } found)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var dependent = await DataViewStore.FirstDependent(_variations, found.Id, ct).ConfigureAwait(false);
        if (dependent.IsFailure) return dependent;
        if (dependent.Value is { } child)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("DataViewHasDependents"), Logger,
                ResultDetails.Create("name", found.VariationName, "reason", $"the view '{child}' is built on it"));
        }

        DataSetVariationLog.DeletingVariation(Logger, found.VariationName, workspace.Value.Name);
        var deleted = await _variations.Delete(found.Id, ct).ConfigureAwait(false);
        if (deleted.IsFailure) return deleted;

        DataSetVariationLog.DeletedVariation(Logger, found.VariationName, workspace.Value.Name);
        return deleted;
    }
}
