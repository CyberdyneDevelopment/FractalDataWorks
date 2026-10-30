using System;
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
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Declines a pending membership request — a named action, not a field change, so this is
/// a raw POST endpoint rather than a <c>CrudUpdateEndpointBase</c> derivative.</summary>
public abstract class DenyWorkspaceMembershipRequestEndpointBase : Endpoint<DenyWorkspaceMembershipRequestRequest, WorkspaceMembershipRequestDto>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger<DenyWorkspaceMembershipRequestEndpointBase> _logger;

    /// <inheritdoc />
    protected DenyWorkspaceMembershipRequestEndpointBase(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IAuthenticationContextAccessor authContext,
        ILogger<DenyWorkspaceMembershipRequestEndpointBase>? logger = null)
    {
        _workspaces = workspaces;
        _access = access;
        _authContext = authContext;
        _logger = logger ?? NullLogger<DenyWorkspaceMembershipRequestEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/workspaces/{Name}/membership-requests/{RequestId}/deny");
        // Signed in is the only gate: MayManageMembers decides, and the owner, a member whose role
        // manages members, or a holder of workspaces:members each pass it (FDW-1071).
#if DEVELOP
        AllowAnonymous();
#endif
        Summary(s =>
        {
            s.Summary = "Decline a membership request";
            s.Description = "Marks the request Declined. No membership is granted.";
        });
        Tags("Workspaces");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DenyWorkspaceMembershipRequestRequest req, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(req.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) { await SendError(workspace, ct).ConfigureAwait(false); return; }
        if (workspace.Value is null) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }

        var permitted = await _access.MayManageMembers(workspace.Value, ct).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.Hides(permitted)) { await Send.NotFoundAsync(ct).ConfigureAwait(false); return; }
        if (permitted.IsFailure) { await SendError(permitted, ct).ConfigureAwait(false); return; }

        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var reviewedByUserId))
        {
            await SendError(GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), _logger,
                ResultDetails.Create("name", req.Name, "reason", "the caller could not be resolved")), ct).ConfigureAwait(false);
            return;
        }

        var lookup = WorkspaceMembershipRequestLookup.FindPending(workspace.Value, _logger, req.Name, req.RequestId);
        if (lookup.IsFailure) { await SendError(lookup, ct).ConfigureAwait(false); return; }

        var found = lookup.Value!;
        found.Status = "Declined";
        found.ReviewedByUserId = reviewedByUserId;
        found.ReviewedAt = DateTimeOffset.UtcNow;
        found.ReviewNotes = req.ReviewNotes;

        // The request is a child of the workspace aggregate; found is the instance inside it.
        var saved = await _workspaces.Update(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) { await SendError(saved, ct).ConfigureAwait(false); return; }

        await Send.OkAsync(WorkspaceMembershipRequestDtos.From(found), ct).ConfigureAwait(false);
    }

    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
