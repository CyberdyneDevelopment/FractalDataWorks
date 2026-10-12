using Fdw.Results;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Checks a workspace's lifecycle values against the collections that define them.
/// </summary>
/// <remarks>
/// The database CHECK constraints refuse the same values, but only after the round trip and with a
/// message naming a constraint rather than a field. This is where a caller finds out what they got
/// wrong. The valid set is not restated here — it is read from the collections, so adding an option
/// does not need this file edited.
/// </remarks>
internal static class WorkspaceLifecycleValidator
{
    /// <summary>Validates a status, if one was supplied.</summary>
    /// <param name="workspaceName">The workspace being written, for the message.</param>
    /// <param name="status">The status, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateStatus(string workspaceName, string? status, ILogger logger) =>
        status is null || !ReferenceEquals(WorkspaceStatuses.ByName(status), WorkspaceStatuses.NotFound)
            ? GenericResult.Success()
            : Reject(workspaceName, "Status", status, logger);

    /// <summary>Validates a visibility, if one was supplied.</summary>
    /// <param name="workspaceName">The workspace being written, for the message.</param>
    /// <param name="visibility">The visibility, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateVisibility(string workspaceName, string? visibility, ILogger logger) =>
        visibility is null || !ReferenceEquals(WorkspaceVisibilities.ByName(visibility), WorkspaceVisibilities.NotFound)
            ? GenericResult.Success()
            : Reject(workspaceName, "Visibility", visibility, logger);

    /// <summary>Validates a join policy, if one was supplied.</summary>
    /// <param name="workspaceName">The workspace being written, for the message.</param>
    /// <param name="joinPolicy">The join policy, or null to skip.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateJoinPolicy(string workspaceName, string? joinPolicy, ILogger logger) =>
        joinPolicy is null || !ReferenceEquals(WorkspaceJoinPolicies.ByName(joinPolicy), WorkspaceJoinPolicies.NotFound)
            ? GenericResult.Success()
            : Reject(workspaceName, "JoinPolicy", joinPolicy, logger);

    /// <summary>
    /// Validates the role an AutoApprove workspace grants, against the join policy it would be stored with.
    /// </summary>
    /// <remarks>
    /// Required when the policy auto-approves and refused otherwise, so the stored value always means
    /// what the policy does with it (FDW-1070). A role that manages members (Owner, Steward) is never
    /// handed to whoever asks: that would let anyone who can find the workspace decide who else is in it.
    /// </remarks>
    /// <param name="workspaceName">The workspace being written, for the message.</param>
    /// <param name="joinPolicy">The join policy the workspace will have.</param>
    /// <param name="autoApproveRole">The role it will grant on auto-approval, or null.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult ValidateAutoApproveRole(string workspaceName, string joinPolicy, string? autoApproveRole, ILogger logger)
    {
        if (!WorkspaceJoinPolicies.ByName(joinPolicy).AutoApproves)
        {
            return autoApproveRole is null
                ? GenericResult.Success()
                : RejectAutoApproveRole(workspaceName, autoApproveRole, $"applies only to an AutoApprove join policy, not '{joinPolicy}'", logger);
        }

        if (string.IsNullOrWhiteSpace(autoApproveRole))
            return RejectAutoApproveRole(workspaceName, string.Empty, "is required when the join policy is AutoApprove", logger);

        var role = WorkspaceMemberRoles.ByName(autoApproveRole);
        if (ReferenceEquals(role, WorkspaceMemberRoles.NotFound))
            return RejectAutoApproveRole(workspaceName, autoApproveRole, "is not a member role", logger);

        return role.MayManageMembers
            ? RejectAutoApproveRole(workspaceName, autoApproveRole, "manages members, and is never granted to whoever asks", logger)
            : GenericResult.Success();
    }

    private static IGenericResult RejectAutoApproveRole(string workspaceName, string value, string reason, ILogger logger) =>
        GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceAutoApproveRoleInvalid"), logger,
            ResultDetails.Create("name", workspaceName, "value", value, "reason", reason));

    private static IGenericResult Reject(string workspaceName, string field, string value, ILogger logger) =>
        GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceLifecycleValueInvalid"), logger,
            ResultDetails.Create("name", workspaceName, "field", field, "value", value));
}
