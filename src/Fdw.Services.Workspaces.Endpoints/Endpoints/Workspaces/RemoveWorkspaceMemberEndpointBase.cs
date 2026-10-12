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
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Removes a member from a workspace.</summary>
/// <remarks>Soft-deletes the one row by its own id, not by re-saving the aggregate — see
/// <see cref="AddWorkspaceMemberEndpointBase"/> for why.</remarks>
public abstract class RemoveWorkspaceMemberEndpointBase : Endpoint<RemoveWorkspaceMemberRequest>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly ILogger<RemoveWorkspaceMemberEndpointBase> _logger;

    /// <inheritdoc />
    protected RemoveWorkspaceMemberEndpointBase(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        ILogger<RemoveWorkspaceMemberEndpointBase>? logger = null)
    {
        _workspaces = workspaces;
        _access = access;
        _logger = logger ?? NullLogger<RemoveWorkspaceMemberEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/workspaces/{Name}/members/{MemberId}");
        // Signed in is the only gate: MayManageRole decides, for the role the member holds -- the owner,
        // a member whose role manages members and ranks at or above it, or a holder of
        // workspaces:members (FDW-1071). The owner's own membership is never removed.
#if DEVELOP
        AllowAnonymous();
#endif
        Summary(s =>
        {
            s.Summary = "Remove a member from a workspace";
            s.Description = "Revokes a user's or role's membership of the workspace.";
        });
        Tags("Workspaces");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(RemoveWorkspaceMemberRequest req, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(req.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure)
        {
            await SendFailure(500, workspace.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        if (workspace.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var permitted = await _access.MayManageMembers(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            // A workspace the caller may not view answers exactly as a missing one does.
            if (WorkspaceAccessRefusals.Hides(permitted))
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
            else
                await SendFailure(ResultHttpStatusMapper.Map(permitted, HttpContext).StatusCode, permitted.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        // Found in the workspace's composed members: the row hangs from the workspace by RowId.
        var found = workspace.Value.Members.Where(x => x.Id == req.MemberId).ToList();
        if (found.Count == 0)
        {
            var notFound = GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), _logger,
                ResultDetails.Create("name", req.Name, "kind", "member", "id", req.MemberId.ToString()));
            await SendFailure(404, notFound.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var removable = await MayRemove(workspace.Value, found[0], ct).ConfigureAwait(false);
        if (removable.IsFailure)
        {
            // Mapped whole, so a refusal that carries its reason (the owner guard) reaches the caller with it.
            var (statusCode, problem) = ResultHttpStatusMapper.Map(removable, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        // The member is a child of the workspace aggregate: saving the workspace without it retires the
        // version that had it, and the new version is written without it.
        workspace.Value.Members.Remove(found[0]);
        var saved = await _workspaces.Save(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            await SendFailure(500, saved.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    // Within the caller's rank, and never the owner's own membership.
    private async Task<IGenericResult> MayRemove(IWorkspaceImplementationConfiguration workspace, WorkspaceMemberConfiguration member, CancellationToken ct)
    {
        var permitted = await _access.MayManageRole(workspace, WorkspaceMemberRoles.ByName(member.MemberRole), ct).ConfigureAwait(false);
        return permitted.IsFailure ? permitted : WorkspaceOwnerMembership.MayRemove(workspace, member, _logger);
    }

    private async Task SendFailure(int statusCode, string? reason, CancellationToken ct)
    {
        await Send.ResultAsync(TypedResults.Json(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = "Member was not removed",
            Detail = reason,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, statusCode: statusCode)).ConfigureAwait(false);
    }
}
