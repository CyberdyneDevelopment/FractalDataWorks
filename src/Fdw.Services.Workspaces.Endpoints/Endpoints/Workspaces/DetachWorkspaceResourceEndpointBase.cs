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
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Detaches a resource from a workspace — removes one node from its map.</summary>
/// <remarks>Soft-deletes the one row by attachment id, not by re-saving the aggregate — see
/// <see cref="AttachWorkspaceResourceEndpointBase"/> for why.</remarks>
public abstract class DetachWorkspaceResourceEndpointBase : Endpoint<DetachWorkspaceResourceRequest>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly ILogger<DetachWorkspaceResourceEndpointBase> _logger;

    /// <inheritdoc />
    protected DetachWorkspaceResourceEndpointBase(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        ILogger<DetachWorkspaceResourceEndpointBase>? logger = null)
    {
        _workspaces = workspaces;
        _access = access;
        _logger = logger ?? NullLogger<DetachWorkspaceResourceEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/workspaces/{Name}/resources/{ResourceAttachmentId}");
        // Signed in is the only gate: MayWrite decides, and the owner, a member whose role may change
        // the workspace, or a holder of workspaces:write each pass it (FDW-936).
#if DEVELOP
        AllowAnonymous();
#endif
        Summary(s =>
        {
            s.Summary = "Detach a resource from a workspace";
            s.Description = "Removes one node from the workspace's map.";
        });
        Tags("Workspaces");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DetachWorkspaceResourceRequest req, CancellationToken ct)
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

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            // A workspace the caller may not view answers exactly as a missing one does.
            if (WorkspaceAccessRefusals.Hides(permitted))
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
            else
                await SendFailure(ResultHttpStatusMapper.Map(permitted, HttpContext).StatusCode, permitted.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        // Found in the workspace's composed resources: the row hangs from the workspace by RowId.
        var found = workspace.Value.Resources.Where(x => x.Id == req.ResourceAttachmentId).ToList();
        if (found.Count == 0)
        {
            var notFound = GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), _logger,
                ResultDetails.Create("name", req.Name, "kind", "resource", "id", req.ResourceAttachmentId.ToString()));
            await SendFailure(404, notFound.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        // The resource is a child of the workspace aggregate: saving the workspace without it retires the
        // version that had it, and the new version is written without it.
        workspace.Value.Resources.Remove(found[0]);
        var saved = await _workspaces.Update(
            workspace.Value, workspace.Value.Domain, workspace.Value.Implementation, workspace.Value.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            await SendFailure(500, saved.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }

    private async Task SendFailure(int statusCode, string? reason, CancellationToken ct)
    {
        await Send.ResultAsync(TypedResults.Json(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = "Resource was not detached",
            Detail = reason,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, statusCode: statusCode)).ConfigureAwait(false);
    }
}
