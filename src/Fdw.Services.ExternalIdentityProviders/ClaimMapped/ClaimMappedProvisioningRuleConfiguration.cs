using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// One matchable rule of <see cref="ClaimMappedExternalIdentityProvisionerConfiguration"/> — a claim
/// to look for on the presented <see cref="System.Security.Claims.ClaimsPrincipal"/>, and what to do
/// when it matches. Ordered children, mirroring
/// <see cref="Fdw.Services.ExternalIdentityProviders.Chained.ChainedProvisionerStepConfiguration"/>:
/// the read cascade does not apply an ORDER BY, so <see cref="ClaimMapped.ClaimMappedProvisioner"/>
/// sorts <see cref="ExecutionOrder"/> ascending itself before matching. The first matching rule wins;
/// no matching rule is this provisioner's "not mine" outcome, not a hard failure.
/// </summary>
/// <remarks>
/// Everything here is configuration, deliberately — no claim type, claim value, role name, or
/// username/email source is a literal anywhere in code. A deployment that wants a second
/// auto-provisioning rule adds a row; it never touches this package.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner")]
public sealed partial class ClaimMappedProvisioningRuleConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "ExternalIdentityProvisioners";

    /// <inheritdoc />
    public string ServiceType => "ExternalIdentityProvisioner";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the display name for this rule.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier for this rule.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the owning ClaimMappedExternalIdentityProvisioner (logical FK
    /// to sec.ClaimMappedExternalIdentityProvisioner.Id).
    /// </summary>
    public Guid ClaimMappedExternalIdentityProvisionerId { get; set; }

    /// <summary>Gets or sets the claim type this rule matches on (e.g. a custom "AutoAdmin" claim).</summary>
    public string ClaimType { get; set; } = string.Empty;

    /// <summary>Gets or sets the claim value this rule requires, matched ordinally.</summary>
    public string ClaimValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the FDW role names granted when this rule matches, comma separated.</summary>
    /// <remarks>
    /// Mirrors <c>JwtBearerAuthenticationConfiguration.Roles</c> exactly: these are FDW role names
    /// that expand to permissions through <c>authz.RolePermission</c>, the same expansion a signed-in
    /// user's roles go through.
    /// </remarks>
    public string Roles { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the claim type read for the new account's username. Required with no default: a
    /// provisioner that invented a username from the subject identifier when this was unset would be
    /// making a choice the deployment never made.
    /// </summary>
    public string UsernameClaimType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the claim type read for the new account's email, or null if this rule's accounts
    /// carry no email.
    /// </summary>
    public string? EmailClaimType { get; set; }

    /// <summary>
    /// Gets or sets the tenant a matched account is created in. Required with no default:
    /// <c>UserConfigurationProvider.CreateUser</c> takes a non-nullable tenant, and inventing one
    /// (a well-known "global" sentinel Guid, say) would be exactly the fallback the no-fallbacks
    /// rule exists to catch — which tenant an auto-provisioned account belongs to is the deployment's
    /// decision to make per rule, not this provisioner's to assume.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the ascending order in which this rule is tried.</summary>
    public int ExecutionOrder { get; set; }
}
