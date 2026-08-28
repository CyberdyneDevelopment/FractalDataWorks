using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Header configuration for identity services representing the <c>sec.Identity</c> parent table —
/// the identities this process can assume when calling out to a peer.
/// </summary>
/// <remarks>
/// <para>
/// Identity-only, per the polymorphic configuration pattern: everything a factory reads at runtime
/// lives on the typed body, because runtime dispatch reads only the typed body and parent fields are
/// discarded after dispatch.
/// </para>
/// <para>
/// After loading a header row, <c>IdentityServiceConfigurationProvider</c> dispatches to the typed
/// body provider and sets <see cref="Configuration"/>. Callers read typed fields by casting, e.g.
/// <c>(header.Configuration as ClientCredentialsConfiguration)</c>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Identity")]
public partial class IdentityServiceConfiguration : IIdentityServiceConfiguration, IServiceDispatchHost
{
    /// <inheritdoc/>
    IGenericConfiguration? IServiceDispatchHost.ServiceDispatchBody => Configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityServiceConfiguration"/> class.
    /// </summary>
    public IdentityServiceConfiguration()
    {
        ServiceType = "Identity";
        SectionName = "Identities";
    }

    /// <summary>
    /// Gets or sets the durable logical identity across versions.
    /// No default — the database assigns identity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name this identity is resolved by. This is the name a caller asks the
    /// provider for (e.g. <c>"SchedulerServiceIdentity"</c>), not the subject the IdP knows.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the IConfiguration section name used for bootstrap binding.</summary>
    public string SectionName { get; set; }

    /// <summary>Gets or sets the service-type domain — always <c>"Identity"</c> for this hierarchy.</summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the TypeOption discriminator. <see cref="IdentityServiceTypes"/> uses this value
    /// to select the mechanism (e.g. <c>"ClientCredentials"</c>, <c>"JwtAssertion"</c>).
    /// </summary>
    [ValuesFrom(typeof(IdentityServiceTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets an optional human-readable description for this configuration.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the typed configuration body for this header row.
    /// Populated on the read path by the provider after loading the typed body table row. Not
    /// persisted — the typed body is saved separately.
    /// </summary>
    [NotMapped]
    public IIdentityServiceImplementationConfiguration? Configuration { get; set; }

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
