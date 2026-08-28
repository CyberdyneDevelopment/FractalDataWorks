using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Header configuration for external identity provisioner services representing the
/// <c>sec.ExternalIdentityProvisioner</c> parent table — identity-only: <see cref="SectionName"/> and
/// <see cref="ServiceType"/> are fixed, get-only computed values (mirroring
/// <c>Fdw.Operations.Configuration.EscalationPolicyConfiguration</c>'s root-config shape), NOT persisted
/// columns — sec.ExternalIdentityProvisioner carries no SectionName/ServiceType column. A
/// tenant/visibility/audit block follows (see <see cref="TenantId"/> through
/// <see cref="ModifyOnBehalfOf"/>). NO secret column exists here — this domain is a
/// provisioning-mechanism selector, not a credential.
/// </summary>
/// <remarks>
/// After loading a header row, <c>ExternalIdentityProvisionerConfigurationProvider</c> dispatches to
/// the typed-body provider and sets <see cref="Configuration"/>. Callers read typed fields by casting,
/// e.g. <c>(header.Configuration as ChainedExternalIdentityProvisionerConfiguration)</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner")]
public partial class ExternalIdentityProvisionerConfiguration : IExternalIdentityProvisionerConfiguration
{
    /// <inheritdoc />
    public string SectionName => "ExternalIdentityProvisioners";

    /// <inheritdoc />
    public string ServiceType => "ExternalIdentityProvisioner";

    /// <summary>
    /// Gets or sets the durable logical identity across versions.
    /// No default — the database assigns identity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name of this external identity provisioner configuration.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TypeOption discriminator. <see cref="ExternalIdentityProvisionerTypes"/> uses
    /// this value to select the active implementation (e.g. <c>"Chained"</c>).
    /// </summary>
    [ValuesFrom(typeof(ExternalIdentityProvisionerTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets an optional human-readable description for this configuration.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the typed configuration body for this header row.
    /// Populated on the read path by the provider after loading the typed body table row. Not
    /// persisted — the typed body is saved separately.
    /// </summary>
    [NotMapped]
    public IExternalIdentityProvisionerImplementationConfiguration? Configuration { get; set; }

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
