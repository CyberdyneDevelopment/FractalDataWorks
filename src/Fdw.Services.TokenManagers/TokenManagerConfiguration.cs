using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Header configuration for token manager services representing the <c>auth.TokenManager</c> parent
/// table. Fields mirror the current <c>AuthenticationServiceConfiguration</c> header set, plus a
/// tenant/visibility/audit block (see <see cref="TenantId"/> through <see cref="ModifyOnBehalfOf"/>).
/// </summary>
/// <remarks>
/// After loading a header row, <c>TokenManagerConfigurationProvider</c> dispatches to the typed-body
/// provider and sets <see cref="Configuration"/>. Callers read typed fields by casting, e.g.
/// <c>(header.Configuration as OpenIddictTokenManagerConfiguration)</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "TokenManager")]
public partial class TokenManagerConfiguration : ITokenManagerConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenManagerConfiguration"/> class.
    /// </summary>
    public TokenManagerConfiguration()
    {
        ServiceType = "TokenManager";
        SectionName = "TokenManagers";
    }

    /// <summary>
    /// Gets or sets the durable logical identity across versions.
    /// No default — the database assigns identity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name of this token manager configuration.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the IConfiguration section name used for bootstrap (configurationSchema.json) binding.</summary>
    public string SectionName { get; set; }

    /// <summary>Gets or sets the service-type domain — always <c>"TokenManager"</c> for this hierarchy.</summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the TypeOption discriminator. <see cref="TokenManagerTypes"/> uses this value to
    /// select the active implementation (e.g. <c>"OpenIddict"</c>).
    /// </summary>
    [ValuesFrom(typeof(TokenManagerTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the secret manager name used to resolve provider secrets (e.g. the signing key).</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the secret key name within the secret manager.</summary>
    public string? SecretKeyName { get; set; }

    /// <summary>Gets or sets an optional human-readable description for this configuration.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the typed configuration body for this header row.
    /// Populated on the read path by the provider after loading the typed body table row. Not
    /// persisted — the typed body is saved separately.
    /// </summary>
    [NotMapped]
    public ITokenManagerImplementationConfiguration? Configuration { get; set; }

    // ── Tenant / visibility / audit ──────────────────────────────────────────

    /// <summary>
    /// Gets or sets the tenant identifier for tenant isolation. Null means system-wide (visible to
    /// all tenants).
    /// </summary>
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
