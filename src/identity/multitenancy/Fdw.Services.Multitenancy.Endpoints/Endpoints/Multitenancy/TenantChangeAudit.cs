using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The audit entries for changes to tenants (FDW-1578). Only a platform administrator changes a tenant, so every entry is at the
/// platform level.
/// </summary>
internal static class TenantChangeAudit
{
    private const string TenantItemKind = "Tenant";

    internal static Task<IGenericResult> Record(IAdministrativeAudit audit, string changeKind, Guid tenantId, string? oldValue, string? newValue,
        CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, TenantItemKind, tenantId.ToString(), AdministrativeLevels.ByName("Platform"), null,
                oldValue, newValue, null),
            cancellationToken);
}
