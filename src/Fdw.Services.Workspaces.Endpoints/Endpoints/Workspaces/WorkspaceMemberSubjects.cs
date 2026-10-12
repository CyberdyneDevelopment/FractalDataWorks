using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Checks that a membership names a user or role the caller's tenant actually has.</summary>
/// <remarks>
/// Read as the caller, so row-level security answers "of this tenant": a user or a tenant-scoped role
/// the caller cannot see does not resolve. A member row naming nothing grants nothing to anyone and hides
/// the mistake, so it is refused with its reason (FDW-1071). An unreadable store is reported as the
/// failure it is, never as "unknown".
/// </remarks>
internal static class WorkspaceMemberSubjects
{
    /// <summary>Resolves the subject a membership names.</summary>
    /// <param name="workspaceName">The workspace, for the refusal.</param>
    /// <param name="subjectType">User or Role.</param>
    /// <param name="subjectId">The user or role.</param>
    /// <param name="users">Reads users as the caller.</param>
    /// <param name="roles">Reads roles as the caller.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success when the subject resolves; a refusal naming it when it does not.</returns>
    internal static async Task<IGenericResult> Resolve(
        string workspaceName, string subjectType, Guid subjectId,
        IUserConfigurationProvider users, IRoleConfigurationProvider roles, ILogger logger, CancellationToken ct)
    {
        var kind = WorkspaceSubjectTypes.ByName(subjectType);
        if (ReferenceEquals(kind, WorkspaceSubjectTypes.User))
        {
            var user = await users.Get(subjectId, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            return user.IsFailure ? user : Found(user.Value is not null, workspaceName, subjectType, subjectId, logger);
        }

        if (ReferenceEquals(kind, WorkspaceSubjectTypes.Role))
        {
            var role = await roles.Get(subjectId, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
            return role.IsFailure ? role : Found(role.Value is not null, workspaceName, subjectType, subjectId, logger);
        }

        return Found(false, workspaceName, subjectType, subjectId, logger);
    }

    private static IGenericResult Found(bool found, string workspaceName, string subjectType, Guid subjectId, ILogger logger) =>
        found
            ? GenericResult.Success()
            : GenericResult.Failure(
                WorkspacesResultCodes.ByName("WorkspaceMemberSubjectUnknown"), logger,
                ResultDetails.Create("name", workspaceName, "subjectType", subjectType, "subjectId", subjectId.ToString("D")));
}
