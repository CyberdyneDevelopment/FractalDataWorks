using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The audit entries for organizations made and changed (FDW-1758), at the Organization level with the organization named.
/// </summary>
internal static class OrganizationChangeAudit
{
    private const string OrganizationItemKind = "Organization";

    internal static Task<IGenericResult> Record(IAdministrativeAudit audit, string changeKind, Guid organizationId, string? oldValue, string? newValue,
        CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, OrganizationItemKind, organizationId.ToString(), AdministrativeLevels.ByName("Organization"), organizationId,
                oldValue, newValue, null),
            cancellationToken);
}
