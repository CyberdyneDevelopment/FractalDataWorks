using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// Records a role change in the administrative audit, so every endpoint that changes who holds a role writes the same entry.
/// </summary>
internal static class RoleChangeAudit
{
    private const string UserRoleItemKind = "UserRole";
    private const string RoleItemKind = "Role";

    /// <summary>
    /// Records that a role was given to or taken from a person.
    /// </summary>
    /// <param name="audit">The administrative audit.</param>
    /// <param name="caller">The signed-in caller, whose token says whether they act for the platform.</param>
    /// <param name="changeKind">What happened: <c>RoleAssigned</c> or <c>RoleRevoked</c>.</param>
    /// <param name="userId">The person whose roles changed.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="oldValue">The role the person held before, or null when they did not hold it.</param>
    /// <param name="newValue">The role the person holds after, or null when they no longer hold it.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of recording.</returns>
    internal static Task<IGenericResult> RecordUserRole(IAdministrativeAudit audit, ClaimsPrincipal caller, string changeKind,
        Guid userId, string roleName, string? oldValue, string? newValue, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, UserRoleItemKind, $"{userId}:{roleName}", LevelOf(caller), null,
                oldValue, newValue, null),
            cancellationToken);

    /// <summary>
    /// Records that a role itself was created, changed, given other permissions or deleted.
    /// </summary>
    /// <param name="audit">The administrative audit.</param>
    /// <param name="caller">The signed-in caller, whose token says whether they act for the platform.</param>
    /// <param name="changeKind">What happened: <c>RoleCreated</c>, <c>RoleUpdated</c>, <c>RolePermissionsSet</c> or <c>RoleDeleted</c>.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="oldValue">How the role was before, or null when it did not exist.</param>
    /// <param name="newValue">How the role is after, or null when it no longer exists.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of recording.</returns>
    internal static Task<IGenericResult> RecordRole(IAdministrativeAudit audit, ClaimsPrincipal caller, string changeKind,
        string roleName, string? oldValue, string? newValue, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, RoleItemKind, roleName, LevelOf(caller), null, oldValue, newValue, null),
            cancellationToken);

    /// <summary>
    /// Says how a role reads, for the before and after of an entry.
    /// </summary>
    /// <param name="displayName">The role's display name.</param>
    /// <param name="description">The role's description.</param>
    /// <returns>The two fields as one line.</returns>
    internal static string Describe(string? displayName, string? description)
        => $"DisplayName='{displayName}'; Description='{description}'";

    // Why the claim and not the tenant id: the globalTenant claim is issued only on a token for the global tenant, and
    // it is what the platform scope already reads, so one fact answers "acting for the platform" everywhere.
    private static IAdministrativeLevel LevelOf(ClaimsPrincipal caller)
        => caller.HasClaim(ClaimDefinitions.globalTenant.Name, "true")
            ? AdministrativeLevels.ByName("Platform")
            : AdministrativeLevels.ByName("Tenant");
}
