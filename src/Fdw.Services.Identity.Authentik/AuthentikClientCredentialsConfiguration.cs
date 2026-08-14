using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.Authentik;

/// <summary>
/// Typed configuration body for the Authentik client-credentials mechanism — an Authentik Service
/// Account plus Application/Provider pair, authenticating with a client id and secret.
/// </summary>
/// <remarks>
/// <para>
/// Suited to long-running services that have somewhere durable to keep a client secret.
/// </para>
/// <para>
/// The client secret is <b>not</b> stored here. <see cref="SecretManagerName"/> and
/// <see cref="SecretKeyName"/> name where to resolve it through <c>ISecretManager</c>, the same way
/// every other FDW component reads a secret. A secret value in a configuration row would be a secret
/// at rest in the configuration database, which is the thing this domain exists to reduce.
/// </para>
/// <para>
/// Be precise about what this mechanism achieves: the secret still exists at rest, it has merely
/// moved from "shared with the peer" to "shared with the identity provider". What changes is that
/// only a short-lived token crosses the wire, the peer holds no copy of anything, and revocation is
/// central. Zero-secret-at-rest is <see cref="AuthentikJwtFederationConfiguration"/>'s property, not
/// this one's.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Identity", ServiceType = "AuthentikClientCredentials")]
public partial class AuthentikClientCredentialsConfiguration : IIdentityServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthentikClientCredentialsConfiguration"/> class.
    /// </summary>
    public AuthentikClientCredentialsConfiguration()
    {
        ServiceType = "Identity";
        SectionName = "Identities";
        ServiceOptionType = "AuthentikClientCredentials";
    }

    /// <summary>Gets or sets the durable logical identity across versions. No default — the database assigns identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this identity is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the IConfiguration section name used for bootstrap binding.</summary>
    public string SectionName { get; set; }

    /// <summary>Gets or sets the service-type domain — always <c>"Identity"</c>.</summary>
    public string ServiceType { get; set; }

    /// <summary>Gets or sets the TypeOption discriminator — always <c>"AuthentikClientCredentials"</c>.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the Authentik issuer URL (e.g. <c>https://login.example.dev/application/o/&lt;slug&gt;/</c>).</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets the absolute token endpoint URL this mechanism posts to.</summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>Gets or sets the OAuth2 client id of the Authentik provider backing this identity.</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the name of the secret manager that holds this identity's client secret.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key within the secret manager under which the client secret is stored.</summary>
    public string? SecretKeyName { get; set; }

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
