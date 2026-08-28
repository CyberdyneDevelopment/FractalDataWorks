using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>Value <c>"true"</c> when the token was issued with cross-tenant scope. Enables RLS Mode 2.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ClaimDefinitions), "crossTenant")]
public sealed class CrossTenantClaim : ClaimDefinitionBase
{
    /// <summary>Initializes a new instance of the <see cref="CrossTenantClaim"/> class.</summary>
    public CrossTenantClaim() : base(id: 6, name: "crossTenant", isArray: false, TokenDestinations.AccessToken) { }
}
