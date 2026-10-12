using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Approves a pending membership request — a named action, not a field change, so this is
/// a raw POST endpoint rather than a <c>CrudUpdateEndpointBase</c> derivative.</summary>
/// <remarks>Grants membership in the same call: marks the request Approved and inserts the
/// corresponding <see cref="WorkspaceMemberConfiguration"/> row, the same shape
/// <see cref="RequestWorkspaceMembershipEndpointBase"/> uses for an AutoApprove policy. The reviewer
/// chooses the role granted (<see cref="ApproveWorkspaceMembershipRequestRequest.GrantedRole"/>), and
/// may grant at most their own rank (FDW-1070, FDW-1071).</remarks>
public abstract class ApproveWorkspaceMembershipRequestEndpointBase : Endpoint<ApproveWorkspaceMembershipRequestRequest, WorkspaceMembershipRequestDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger<ApproveWorkspaceMembershipRequestEndpointBase> _logger;

    /// <inheritdoc />
    protected ApproveWorkspaceMembershipRequestEndpointBase(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IAuthenticationContextAccessor authContext,
        ILogger<ApproveWorkspaceMembershipRequestEndpointBase>? logger = null)
    {
        _workspaces = workspaces;
        _access = access;
        _authContext = authContext;
        _logger = logger ?? NullLogger<ApproveWorkspaceMembershipRequestEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/workspaces/{Name}/membership-requests/{RequestId}/approve");
        // Signed in is the only gate: MayManageRole decides, and the owner, a member whose role manages
        // members and ranks at or above the role granted, or a holder of workspaces:members pass it (FDW-1071).
#if DEVELOP
        AllowAnonymous();
#endif
        Summary(s =>
        {
            s.Summary = "Approve a membership request";
            s.Description = "Grants the requested membership and marks the request Approved.";
        });
        Tags("Workspaces");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ApproveWorkspaceMembershipRequestRequest req, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(req.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) { await SendError(workspace, ct).ConfigureAwait(false); return; }
        if (workspace.Value is null) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }

        var manages = await _access.MayManageMembers(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.Hides(manages)) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }
        if (manages.IsFailure) { await SendError(manages, ct).ConfigureAwait(false); return; }

        var granted = await AuthorizeGrant(workspace.Value, req, ct).ConfigureAwait(false);
        if (granted.IsFailure) { await SendError(granted, ct).ConfigureAwait(false); return; }
        var (grantedRole, reviewedByUserId) = granted.Value;

        var lookup = WorkspaceMembershipRequestLookup.FindPending(workspace.Value, _logger, req.Name, req.RequestId);
        if (lookup.IsFailure) { await SendError(lookup, ct).ConfigureAwait(false); return; }

        var found = lookup.Value!;
        var now = DateTimeOffset.UtcNow;
        found.Status = "Approved";
        found.ReviewedByUserId = reviewedByUserId;
        found.ReviewedAt = now;
        found.ReviewNotes = req.ReviewNotes;

        // The request and the membership it grants are children of the workspace aggregate: one save
        // writes both under the workspace's current row.
        WorkspaceMembershipGrant.Grant(workspace.Value, found, grantedRole.Name, reviewedByUserId, now);
        var saved = await _workspaces.Save(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) { await SendError(saved, ct).ConfigureAwait(false); return; }

        await Send.OkAsync(WorkspaceMembershipRequestDtos.From(found), ct).ConfigureAwait(false);
    }

    // The role the reviewer grants, checked against the reviewer's own rank, and the reviewer as a User:
    // everything the grant needs that the request itself cannot be trusted to say (FDW-1070).
    private async Task<IGenericResult<(IWorkspaceMemberRole Role, Guid ReviewerId)>> AuthorizeGrant(
        IWorkspaceImplementationConfiguration workspace, ApproveWorkspaceMembershipRequestRequest req, CancellationToken ct)
    {
        var grantedRole = WorkspaceMemberRoles.ByName(req.GrantedRole);
        if (ReferenceEquals(grantedRole, WorkspaceMemberRoles.NotFound))
        {
            return GenericResult<(IWorkspaceMemberRole, Guid)>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", req.Name, "field", "grantedRole", "value", req.GrantedRole));
        }

        var permitted = await _access.MayManageRole(workspace, grantedRole, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<(IWorkspaceMemberRole, Guid)>();

        return _authContext.Current is { } caller && Guid.TryParse(caller.UserId, out var reviewedByUserId)
            ? GenericResult<(IWorkspaceMemberRole, Guid)>.Success((grantedRole, reviewedByUserId))
            : GenericResult<(IWorkspaceMemberRole, Guid)>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), _logger,
                ResultDetails.Create("name", req.Name, "reason", "the caller could not be resolved"));
    }

    /// <summary>Inserts the WorkspaceMember row, unless the subject already holds a current one.</summary>
    /// <remarks>
    /// Already a member covers re-approving after the subject left and rejoined via a fresh request:
    /// the request is Approved either way, but a second insert would collide with
    /// UX_WorkspaceMember_Workspace_Subject_Current.
    /// </remarks>
    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
