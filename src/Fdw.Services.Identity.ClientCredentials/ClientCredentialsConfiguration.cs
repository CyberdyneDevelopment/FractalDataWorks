using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// Typed configuration body for the client-credentials mechanism — a service proving its identity
/// with an OAuth 2.0 client-credentials grant against any conforming token endpoint. FDW's own
/// OpenIddict authorization server is the usual target.
/// </summary>
/// <remarks>
/// <para>
/// This is the mechanism for service-to-service calls that stay inside the deployment: the scheduler
/// dispatching a pipeline to the ETL server, the ETL server calling back into the API. No external
/// identity provider is involved, so it is the mechanism that can be stood up and proven with nothing
/// but FDW itself.
/// </para>
/// <para>
/// The client secret is <b>not</b> stored here. <see cref="SecretManagerName"/> and
/// <see cref="SecretKeyName"/> name where to resolve it through <c>ISecretManager</c>, the same way
/// every other FDW component reads a secret. A secret value in a configuration row would be a secret
/// at rest in the configuration database, which is the thing this domain exists to reduce.
/// </para>
/// <para>
/// Why this exists as a mechanism rather than as a second outbound abstraction: acquiring a token for
/// this process is one question, and the provider answering it is configuration. FDW previously
/// declared a separate <c>IOutboundCredentialService</c> for exactly this case; it was a second answer
/// to the same question, and callers would have had to know which of the two their peer had been
/// wired with. It never acquired an implementation and has been deleted. <c>TokenEndpoint</c> pointing
/// at one server's <c>/connect/token</c> rather than another's is the entire difference between this
/// mechanism and any other, and that belongs in a configuration row — which is also why this mechanism
/// is named for the technology, not for whose instance it happens to point at.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Identity", ServiceType = "ClientCredentials")]
public partial class ClientCredentialsConfiguration : IIdentityServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsConfiguration"/> class.
    /// </summary>
    public ClientCredentialsConfiguration()
    {
        ServiceType = "Identity";
        SectionName = "Identities";
        ServiceOptionType = "ClientCredentials";
    }

    /// <summary>Gets or sets the durable logical identity across versions. No default — the database assigns identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this identity is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the IConfiguration section name used for bootstrap binding.</summary>
    public string SectionName { get; set; }

    /// <summary>Gets or sets the service-type domain — always <c>"Identity"</c>.</summary>
    public string ServiceType { get; set; }

    /// <summary>Gets or sets the TypeOption discriminator — always <c>"ClientCredentials"</c>.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the issuer URL of the authorization server (e.g. <c>https://api.example.dev/</c>).</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets the absolute token endpoint URL — the server's <c>/connect/token</c>.</summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>Gets or sets the client id registered for this service at that server (e.g. <c>fdw.scheduler</c>).</summary>
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
