using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>
/// The audit entries for changes to the roles and members of an organization (FDW-1593), at the Organization level with the
/// organization named.
/// </summary>
internal static class OrganizationChangeAudit
{
    private const string RoleItemKind = "OrganizationRole";
    private const string MemberItemKind = "OrganizationMember";

    internal static Task<IGenericResult> Role(IAdministrativeAudit audit, string changeKind, Guid organizationId, string roleName,
        string? oldValue, string? newValue, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, RoleItemKind, ItemOf(organizationId, roleName), AdministrativeLevels.ByName("Organization"),
                organizationId, oldValue, newValue, null),
            cancellationToken);

    internal static Task<IGenericResult> Member(IAdministrativeAudit audit, string changeKind, Guid organizationId, Guid userId,
        string? oldValue, string? newValue, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, MemberItemKind, ItemOf(organizationId, userId.ToString()), AdministrativeLevels.ByName("Organization"),
                organizationId, oldValue, newValue, null),
            cancellationToken);

    internal static string ItemOf(Guid organizationId, string name) => $"{organizationId}:{name}";

    internal static string Describe(string? description, System.Collections.Generic.IEnumerable<string> permissions)
        => $"Description='{description}'; Permissions=[{string.Join(", ", permissions)}]";
}
