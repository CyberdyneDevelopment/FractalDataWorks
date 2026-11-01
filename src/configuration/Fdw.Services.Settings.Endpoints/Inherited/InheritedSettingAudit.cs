using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// The audit entries for settings set and locks lifted (FDW-1579). A change is recorded at the level it was made at, and names the
/// tenant or organization it concerns in the item.
/// </summary>
internal static class InheritedSettingAudit
{
    private const string SettingItemKind = "Setting";

    internal static Task<IGenericResult> Record(IAdministrativeAudit audit, string changeKind, string level, string itemId, Guid? organizationId,
        EffectiveSetting? before, EffectiveSetting? after, CancellationToken cancellationToken)
        => audit.Record(
            new AdministrativeChangeEntry(changeKind, SettingItemKind, itemId, AdministrativeLevels.ByName(level), organizationId,
                Describe(before), Describe(after), null),
            cancellationToken);

    internal static string ItemOf(SettingScope scope, string settingName)
        => scope.TenantId is { } tenantId ? $"{tenantId}:{settingName}"
            : scope.OrganizationId is { } organizationId ? $"{organizationId}:{settingName}"
            : settingName;

    private static string? Describe(EffectiveSetting? setting)
        => setting is null ? null : $"Value='{setting.Value}'; SetAt='{setting.SetAt}'; LockedAt='{setting.LockedAt}'";
}
