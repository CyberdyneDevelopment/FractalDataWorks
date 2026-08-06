using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Typed-body configuration for the Chained external-identity-provisioner TypeOption.
/// Standalone POCO — does NOT inherit from <see cref="ExternalIdentityProvisionerConfiguration"/>.
/// Persisted to <c>sec.ChainedExternalIdentityProvisioner</c> as a child of
/// <c>sec.ExternalIdentityProvisioner</c> via <see cref="ExternalIdentityProvisionerId"/>. Carries no
/// scalar columns of its own — its policy lives entirely in the ordered <see cref="Steps"/> child
/// collection (<c>sec.ChainedProvisionerStep</c>), auto-cascaded by
/// <c>DefaultConfigurationProvider.ComposeChildren</c>/<c>CascadeOwnerChildren</c> via the generated
/// mapper's typed-list child detection — no additional attribute is needed beyond <c>[GenerateMapper]</c>
/// on both this type and <see cref="ChainedProvisionerStepConfiguration"/>.
/// </summary>
/// <remarks>
/// Mirrors <c>OidcExternalIdentityProviderConfiguration</c>'s standalone-typed-body shape and
/// <c>Fdw.Operations.Configuration.EscalationPolicyConfiguration</c>'s ordered-children shape
/// (<c>Levels</c> there, <see cref="Steps"/> here).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner", ServiceType = "Chained")]
public sealed partial class ChainedExternalIdentityProvisionerConfiguration : IExternalIdentityProvisionerConfiguration
{
    // ========================================
    // IGenericConfiguration (typed-body identity)
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed-body row
    /// (<c>sec.ChainedExternalIdentityProvisioner.Id</c>).
    /// </summary>
    // Why: No Guid.NewGuid() default — the provider mints this before INSERT.
    public Guid Id { get; set; }

    // Why: IGenericConfiguration members below satisfy the interface contract via EXPLICIT
    // interface implementation so [GenerateMapper] does NOT map them — they are not columns on
    // sec.ChainedExternalIdentityProvisioner. The canonical Name/SectionName/ServiceType/
    // ServiceOptionType live on the parent ExternalIdentityProvisionerConfiguration row; the typed
    // body is identified solely by ExternalIdentityProvisionerId. Mirrors
    // OidcExternalIdentityProviderConfiguration.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — identified by ExternalIdentityProvisionerId */ }
    }

    string IGenericConfiguration.SectionName => "ExternalIdentityProvisioners";
    string IGenericConfiguration.ServiceType => "ExternalIdentityProvisioner";
    string? IGenericConfiguration.ServiceOptionType => "Chained";

    // ========================================
    // FK to header
    // ========================================

    /// <summary>
    /// Gets or sets the durable logical FK to <c>sec.ExternalIdentityProvisioner.Id</c> (the parent header).
    /// </summary>
    public Guid ExternalIdentityProvisionerId { get; set; }

    // ========================================
    // Ordered children
    // ========================================

    /// <summary>
    /// Gets or sets the ordered steps this chain walks. Populated automatically from
    /// sec.ChainedProvisionerStep child rows during configuration loading — NOT pre-sorted by the read
    /// cascade; <see cref="Chained.ChainedExternalIdentityProvisioner"/> sorts by
    /// <see cref="ChainedProvisionerStepConfiguration.ExecutionOrder"/> ascending before walking.
    /// </summary>
    public IList<ChainedProvisionerStepConfiguration> Steps { get; set; } = [];
}
