using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Lists a workspace's members.</summary>
/// <remarks>
/// Reads through the aggregate's composed Members collection, like <see cref="GetWorkspaceMapEndpointBase"/>
/// reads Resources/Relationships -- FDW-795's child-join fix already filters the child rows' own
/// IsCurrent/IsDeleted, so this is not the insert-only-cascade concern that keeps Attach/Create off
/// the aggregate's Save path.
/// </remarks>
public abstract class ListWorkspaceMembersEndpointBase
    : CrudListEndpointBase<WorkspaceNameRequest, WorkspaceMemberDto>
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;

    /// <inheritdoc />
    protected ListWorkspaceMembersEndpointBase(
        ILogger<ListWorkspaceMembersEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _workspaces = workspaces;
        _access = access;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>Who is in a workspace is part of viewing it: <see cref="IWorkspaceAccessPolicy.MayView"/> decides (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/members";

    /// <inheritdoc />
    protected override string EndpointSummary => "List a workspace's members";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns everyone and every role holding membership in the workspace.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<WorkspaceMemberDto>>> LoadItems(
        WorkspaceNameRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<List<WorkspaceMemberDto>>();
        if (workspace.Value is null) return GenericResult<List<WorkspaceMemberDto>>.Success([]);

        var permitted = await _access.MayView(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            // A hidden workspace answers as an absent one does here -- an empty list -- so its name
            // cannot be confirmed by probing; an undetermined decision is passed on.
            return WorkspaceAccessRefusals.Hides(permitted)
                ? GenericResult<List<WorkspaceMemberDto>>.Success([])
                : permitted.ToNewResult<List<WorkspaceMemberDto>>();
        }

        var members = workspace.Value.Members
            .Select(m => new WorkspaceMemberDto
            {
                Id = m.Id,
                SubjectType = m.SubjectType,
                SubjectId = m.SubjectId,
                MemberRole = m.MemberRole,
                State = m.State,
                JoinedAt = m.JoinedAt,
            })
            .ToList();

        return GenericResult<List<WorkspaceMemberDto>>.Success(members);
    }
}
