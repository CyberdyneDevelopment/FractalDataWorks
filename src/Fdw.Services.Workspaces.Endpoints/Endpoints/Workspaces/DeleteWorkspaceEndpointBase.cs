using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Soft-deletes a workspace, and the data set variations it owns with it.</summary>
public abstract class DeleteWorkspaceEndpointBase : CrudDeleteEndpointBase<WorkspaceNameRequest>
{
    private readonly IWorkspaceConfigurationProvider _provider;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <inheritdoc />
    protected DeleteWorkspaceEndpointBase(
        ILogger<DeleteWorkspaceEndpointBase> logger,
        IWorkspaceConfigurationProvider provider,
        IWorkspaceAccessPolicy access,
        IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _provider = provider;
        _access = access;
        _variations = variations;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WorkspaceNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(
        WorkspaceNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(WorkspaceNameRequest request, CancellationToken ct)
    {
        var found = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found;

        // The base already established existence, so a null here is an inconsistency, not a 404.
        if (found.Value is null)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        // Deleting somebody else's project is the sharpest form of the thing workspaces:write used
        // to permit across the whole tenant, so it asks the same question the update does.
        var permitted = await _access.MayWrite(found.Value, ct).ConfigureAwait(false);
        return permitted.IsFailure
            ? permitted
            : await DeleteWithPermissions(found.Value, ct).ConfigureAwait(false);
    }

    /// <summary>Deletes the workspace, then the variations it owns, then its own permissions and their grants.</summary>
    /// <remarks>
    /// The workspace first: if it refuses to go, nothing else has been touched and its role grants
    /// survive. The variations are retired next, because a variation is only its workspace's view of a data set and
    /// nothing else can delete one. The permissions are named by the workspace's Id, so any left behind by a failure after
    /// that can never grant another workspace; the result still reports them, since the delete was not
    /// clean (FDW-937). The service is resolved before anything is deleted.
    /// </remarks>
    private async Task<IGenericResult> DeleteWithPermissions(IWorkspaceImplementationConfiguration workspace, CancellationToken ct)
    {
        if (TryResolve<IWorkspacePermissions>() is not { } permissions)
        {
            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspacePermissionsUnavailable"), Logger,
                ResultDetails.Create("name", workspace.Name, "step", "deleted"));
        }

        var deleted = await _provider.Delete(workspace.Id, ct).ConfigureAwait(false);
        if (deleted.IsFailure) return deleted;

        var retired = await RetireVariations(workspace, ct).ConfigureAwait(false);
        if (retired.IsFailure) return retired;

        var removed = await permissions.Remove(workspace.Id, ct).ConfigureAwait(false);
        if (removed.IsSuccess) return removed;

        return GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspacePermissionsNotRemoved"), Logger,
            ResultDetails.Create("name", workspace.Name, "id", workspace.Id.ToString("D", CultureInfo.InvariantCulture),
                "reason", string.Join(" <- ", removed.CodeChain.Select(code => code.Name))));
    }

    /// <summary>Retires every variation the workspace owns; the first that will not go fails the delete, naming it.</summary>
    private async Task<IGenericResult> RetireVariations(IWorkspaceImplementationConfiguration workspace, CancellationToken ct)
    {
        var owned = await DataSetVariationQueries.OwnedBy(_variations, workspace.Id, ct).ConfigureAwait(false);
        if (owned.IsFailure) return owned;

        foreach (var variation in owned.Value!)
        {
            var retired = await _variations.Delete(variation.Id, ct).ConfigureAwait(false);
            if (retired.IsSuccess) continue;

            return GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceVariationsNotRemoved"), Logger,
                ResultDetails.Create("name", workspace.Name, "variation", variation.Name,
                    "reason", string.Join(" <- ", retired.CodeChain.Select(code => code.Name))));
        }

        return GenericResult.Success();
    }
}
