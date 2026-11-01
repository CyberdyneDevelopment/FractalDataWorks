using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The audit entries for features switched on and off (FDW-1580). A feature switched for a tenant is the platform's act; one switched
/// for an organization is the tenant's but is recorded at the organization level, because an entry that names an organization must be made at that level.
/// </summary>
internal static class FeatureAudit
{
    private const string FeatureItemKind = "Feature";

    internal static Task<IGenericResult> Record(IAdministrativeAudit audit, bool enabled, string level, string itemId, Guid? organizationId,
        CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(enabled ? "FeatureEnabled" : "FeatureDisabled", FeatureItemKind, itemId, AdministrativeLevels.ByName(level), organizationId,
                enabled ? "Off" : "On", enabled ? "On" : "Off", null),
            cancellationToken);
}
