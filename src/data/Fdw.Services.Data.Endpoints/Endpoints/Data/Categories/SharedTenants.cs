using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The tenants whose rows, with no organisation narrowing them, every identified caller can read in every tenant. This mirrors
/// <c>security.fn_TenantFilter</c> ("Shared rows: owned by the global or the default tenant, no visibility group") in ConfigurationDb, which is
/// where the rule is enforced; a category parent rule has to know which parents everyone can see.
/// </summary>
internal static class SharedTenants
{
    private static readonly Guid Global = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
    private static readonly Guid Default = Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE");

    /// <summary>Says whether a tenant's rows with no organisation are readable by everyone.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <returns>True for the global tenant and the default tenant.</returns>
    public static bool Shares(Guid tenantId) => tenantId == Global || tenantId == Default;
}
