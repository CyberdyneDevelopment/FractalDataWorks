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
/// <c>ImplementationConfigurationProviderBase.ComposeChildren</c>/<c>CascadeOwnerChildren</c> via the generated
/// mapper's typed-list child detection — no additional attribute is needed beyond <c>[GenerateMapper]</c>
/// on both this type and <see cref="ChainedProvisionerStepConfiguration"/>.
/// </summary>
/// <remarks>
/// A standalone typed-body shape, same pattern as
/// <c>Fdw.Operations.Configuration.EscalationPolicyConfiguration</c>'s ordered-children shape
/// (<c>Levels</c> there, <see cref="Steps"/> here).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner", ServiceType = "Chained")]
public sealed partial class ChainedExternalIdentityProvisionerConfiguration : IExternalIdentityProvisionerImplementationConfiguration
{
    // ========================================
    // IGenericConfiguration (typed-body identity)
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed-body row
    /// (<c>sec.ChainedExternalIdentityProvisioner.Id</c>).
    /// </summary>
    public Guid Id { get; set; }

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

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;


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
