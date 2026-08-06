using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>Organization identifier within the tenant. Absent for global-tenant admins.</summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "orgId")]
public sealed class OrgIdClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="OrgIdClaim"/> class.</summary>
    public OrgIdClaim() : base(id: 3, name: "orgId", isArray: false, TokenDestinations.AccessToken) { }
}
