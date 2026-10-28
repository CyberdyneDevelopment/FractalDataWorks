using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>Organization identifier within the tenant. Absent for global-tenant admins.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "orgId")]
public sealed class OrgIdClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="OrgIdClaim"/> class.</summary>
    public OrgIdClaim() : base(name: "orgId", isArray: false, isSessionBound: false, TokenDestinations.AccessToken) { }
}
