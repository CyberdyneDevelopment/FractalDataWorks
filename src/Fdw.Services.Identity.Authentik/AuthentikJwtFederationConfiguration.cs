using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Authentik.Assertions;

namespace Fdw.Services.Identity.Authentik;

/// <summary>
/// Typed configuration body for the Authentik federated-JWT mechanism — Authentik trusts an external
/// OIDC issuer's signing keys directly, and this workload exchanges an assertion that issuer already
/// minted for it.
/// </summary>
/// <remarks>
/// <para>
/// <b>No static secret exists anywhere in this mechanism.</b> The assertion is minted per workload
/// (a CI system's per-job OIDC tokens being the motivating case), expires in minutes, and is bound
/// to the workload's identity by the issuing system itself. This is the mechanism to prefer wherever
/// the workload already has a trustworthy issuer.
/// </para>
/// <para>
/// Its precondition is exactly that: something must already be minting per-workload assertions. A
/// long-running service with no such issuer cannot use this and wants
/// <see cref="AuthentikClientCredentialsConfiguration"/> instead.
/// </para>
/// <para>
/// <see cref="AssertionSource"/> names <em>how</em> the incoming assertion is read (a
/// <c>FederatedAssertionSources</c> TypeOption) and <see cref="AssertionLocation"/> names
/// <em>where</em> — an environment variable name, a file path. Splitting carrier from location is
/// what lets a new carrier be added without touching the exchange.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Identity", ServiceType = "AuthentikJwtFederation")]
public partial class AuthentikJwtFederationConfiguration : IIdentityServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthentikJwtFederationConfiguration"/> class.
    /// </summary>
    public AuthentikJwtFederationConfiguration()
    {
        ServiceType = "Identity";
        SectionName = "Identities";
        ServiceOptionType = "AuthentikJwtFederation";
    }

    /// <summary>Gets or sets the durable logical identity across versions. No default — the database assigns identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this identity is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the IConfiguration section name used for bootstrap binding.</summary>
    public string SectionName { get; set; }

    /// <summary>Gets or sets the service-type domain — always <c>"Identity"</c>.</summary>
    public string ServiceType { get; set; }

    /// <summary>Gets or sets the TypeOption discriminator — always <c>"AuthentikJwtFederation"</c>.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the Authentik issuer URL.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets the absolute token endpoint URL this mechanism posts to.</summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>Gets or sets the OAuth2 client id of the Authentik provider backing this identity.</summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the <c>FederatedAssertionSources</c> TypeOption name describing how the incoming
    /// assertion is carried (e.g. <c>EnvironmentVariable</c>, <c>File</c>).
    /// </summary>
    [ValuesFrom(typeof(FederatedAssertionSources))]
    public string? AssertionSource { get; set; }

    /// <summary>
    /// Gets or sets where the assertion source looks — an environment variable name for
    /// <c>EnvironmentVariable</c>, a path for <c>File</c>.
    /// </summary>
    public string? AssertionLocation { get; set; }

    /// <summary>Gets or sets the space-delimited scopes requested when no caller supplies them.</summary>
    public string? Scopes { get; set; }

    /// <summary>Gets or sets an optional human-readable description for this configuration.</summary>
    public string? Description { get; set; }

    // ── Tenant / visibility / audit ──────────────────────────────────────────
    // Why: no value defaults — a missing tenant/visibility/audit value must read as its
    // DB-configured null, never a silently-assumed default.

    /// <summary>Gets or sets the tenant identifier for tenant isolation. Null means system-wide.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional visibility group identifier.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the source create date (set by DB).</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the user who created this record.</summary>
    public string? CreateBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for create (impersonation).</summary>
    public string? CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the last modification date.</summary>
    public DateTimeOffset? ModifyDate { get; set; }

    /// <summary>Gets or sets the user who last modified this record.</summary>
    public string? ModifyBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for modify (impersonation).</summary>
    public string? ModifyOnBehalfOf { get; set; }
}
