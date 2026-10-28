using Fdw.Collections.Attributes;

namespace Fdw.Claims.Abstractions;

/// <summary>Tenant identifier the user is operating under. Feeds SESSION_CONTEXT for RLS.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "tenantId")]
public sealed class TenantIdClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantIdClaim"/> class.</summary>
    public TenantIdClaim() : base(name: "tenantId", isArray: false, isSessionBound: false, TokenDestinations.AccessToken) { }
}
