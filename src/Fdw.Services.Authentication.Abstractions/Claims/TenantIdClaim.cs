using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>Tenant identifier the user is operating under. Feeds SESSION_CONTEXT for RLS.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "tenantId")]
public sealed class TenantIdClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantIdClaim"/> class.</summary>
    public TenantIdClaim() : base(id: 2, name: "tenantId", isArray: false, TokenDestinations.AccessToken) { }
}
