using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Lists a workspace's membership requests.</summary>
/// <remarks>
/// WorkspaceMembershipRequestConfiguration is not composed onto the aggregate (unlike
/// Members/Resources/Relationships), so this reads directly through the gateway, the same shape as
/// the write-side endpoints in this file group.
/// </remarks>
public abstract class ListWorkspaceMembershipRequestsEndpointBase
    : CrudListEndpointBase<WorkspaceNameRequest, WorkspaceMembershipRequestDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;

    /// <inheritdoc />
    protected ListWorkspaceMembershipRequestsEndpointBase(
        ILogger<ListWorkspaceMembershipRequestsEndpointBase> logger,
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>The requests are for the people who decide them, so <see cref="IWorkspaceAccessPolicy.MayManageMembers"/> decides -- the same test approving or denying one applies (FDW-1071). Viewing is not enough: an Open workspace is viewable by everyone in the tenant, and who asked to join is not theirs to read (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/membership-requests";

    /// <inheritdoc />
    protected override string EndpointSummary => "List a workspace's membership requests";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns every request to join the workspace, reviewed or not.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<WorkspaceMembershipRequestDto>>> LoadItems(
        WorkspaceNameRequest request, CancellationToken ct)
    {
        // One level down: the requests and the members the access policy reads, not the workspace's
        // resources, relationships or notes (FDW-951).
        var workspace = await _workspaces.Get(request.Name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<List<WorkspaceMembershipRequestDto>>();
        if (workspace.Value is null) return GenericResult<List<WorkspaceMembershipRequestDto>>.Success([]);

        var permitted = await _access.MayManageMembers(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            // A hidden workspace answers as an absent one does here -- an empty list -- so its name
            // cannot be confirmed by probing; one they may view but not change answers 403.
            return WorkspaceAccessRefusals.Hides(permitted)
                ? GenericResult<List<WorkspaceMembershipRequestDto>>.Success([])
                : permitted.ToNewResult<List<WorkspaceMembershipRequestDto>>();
        }

        // The requests are children of the workspace aggregate, composed by the same read.
        var requests = workspace.Value.MembershipRequests
            .Select(WorkspaceMembershipRequestDtos.From)
            .ToList();

        return GenericResult<List<WorkspaceMembershipRequestDto>>.Success(requests);
    }
}
