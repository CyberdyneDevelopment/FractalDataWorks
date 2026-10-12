using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Finds one Pending membership request, or the reason it can't be reviewed.</summary>
/// <remarks>
/// Shared by <see cref="ApproveWorkspaceMembershipRequestEndpointBase"/> and
/// <see cref="DenyWorkspaceMembershipRequestEndpointBase"/> — both need the same row before either
/// can act on it, and the same two refusals (missing, already reviewed) either way.
/// </remarks>
internal static class WorkspaceMembershipRequestLookup
{
    /// <summary>Finds the request among the workspace's composed requests and confirms it is still Pending.</summary>
    /// <param name="workspace">The workspace aggregate, read with its membership requests.</param>
    /// <param name="logger">Logs the refusal.</param>
    /// <param name="workspaceName">The workspace's name, for the refusal's details.</param>
    /// <param name="requestId">The request to find.</param>
    /// <returns>The request, or why it cannot be reviewed.</returns>
    public static IGenericResult<WorkspaceMembershipRequestConfiguration> FindPending(
        IWorkspaceImplementationConfiguration workspace,
        ILogger logger,
        string workspaceName,
        Guid requestId)
    {
        var found = workspace.MembershipRequests.FirstOrDefault(r => r.Id == requestId);
        if (found is null)
        {
            return GenericResult<WorkspaceMembershipRequestConfiguration>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceChildNotFound"), logger,
                ResultDetails.Create("name", workspaceName, "kind", "membership request", "id", requestId.ToString()));
        }

        if (!string.Equals(found.Status, "Pending", StringComparison.Ordinal))
        {
            return GenericResult<WorkspaceMembershipRequestConfiguration>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), logger,
                ResultDetails.Create("name", workspaceName, "field", "status", "value", found.Status));
        }

        return GenericResult<WorkspaceMembershipRequestConfiguration>.Success(found);
    }
}
