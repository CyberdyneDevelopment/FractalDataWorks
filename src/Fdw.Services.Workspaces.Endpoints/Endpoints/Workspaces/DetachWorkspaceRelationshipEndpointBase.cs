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

/// <summary>Removes a declared relationship from a workspace — removes one edge from its map.</summary>
/// <remarks>Soft-deletes the one row by its own id, not by re-saving the aggregate — see
/// <see cref="CreateWorkspaceRelationshipEndpointBase"/> for why.</remarks>
public abstract class DetachWorkspaceRelationshipEndpointBase : Endpoint<DetachWorkspaceRelationshipRequest>
{
    private readonly WorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly ILogger<DetachWorkspaceRelationshipEndpointBase> _logger;

    /// <inheritdoc />
    protected DetachWorkspaceRelationshipEndpointBase(
        WorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        ILogger<DetachWorkspaceRelationshipEndpointBase>? logger = null)
    {
        _workspaces = workspaces;
        _access = access;
        _logger = logger ?? NullLogger<DetachWorkspaceRelationshipEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/workspaces/{Name}/relationships/{RelationshipId}");
        // Signed in is the only gate: MayWrite decides, and the owner, a member whose role may change
        // the workspace, or a holder of workspaces:write each pass it (FDW-936).
#if DEVELOP
        AllowAnonymous();
#endif
        Summary(s =>
        {
            s.Summary = "Remove a relationship from a workspace";
            s.Description = "Removes one edge from the workspace's map.";
        });
        Tags("Workspaces");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(DetachWorkspaceRelationshipRequest req, CancellationToken ct)
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

        // Found in the workspace's composed relationships: the row hangs from the workspace by RowId.
        var found = workspace.Value.Relationships.Where(x => x.Id == req.RelationshipId).ToList();
        if (found.Count == 0)
        {
            var notFound = GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), _logger,
                ResultDetails.Create("name", req.Name, "kind", "relationship", "id", req.RelationshipId.ToString()));
            await SendFailure(404, notFound.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        // The relationship is a child of the workspace aggregate: saving the workspace without it retires the
        // version that had it, and the new version is written without it.
        workspace.Value.Relationships.Remove(found[0]);
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
            Title = "Relationship was not removed",
            Detail = reason,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, statusCode: statusCode)).ConfigureAwait(false);
    }
}
