using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The audit entries for retention policies, legal holds and documents ended by the retention job (FDW-1575), at the Organization level
/// with the organization named.
/// </summary>
internal static class RetentionAudit
{
    internal static Task<IGenericResult> Record(IAdministrativeAudit audit, string changeKind, string itemKind, string itemId, Guid organizationId,
        string? oldValue, string? newValue, string? reason, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, itemKind, itemId, AdministrativeLevels.ByName("Organization"), organizationId, oldValue, newValue, reason),
            cancellationToken);
}
