using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>
/// Value <c>"true"</c> when the token was issued for the global tenant — the tenant whose members
/// see and operate on every tenant's data. Absent on a token for any other tenant.
/// </summary>
/// <remarks>
/// Carried so that "is this caller acting as the platform" is answerable from the token alone,
/// without a tenant lookup on every request: an Admin acting in the global tenant administers the
/// platform, an Admin acting in any other tenant administers only that tenant (FDW-825).
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "globalTenant")]
public sealed class GlobalTenantClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="GlobalTenantClaim"/> class.</summary>
    public GlobalTenantClaim() : base(name: "globalTenant", isArray: false, isSessionBound: true, TokenDestinations.AccessToken) { }
}
