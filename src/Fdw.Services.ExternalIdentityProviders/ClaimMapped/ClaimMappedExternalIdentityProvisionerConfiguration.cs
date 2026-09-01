using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// Typed-body configuration for the ClaimMapped external-identity-provisioner TypeOption.
/// Standalone POCO — does NOT inherit from <see cref="ExternalIdentityProvisionerConfiguration"/>.
/// Persisted to <c>sec.ClaimMappedExternalIdentityProvisioner</c> as a child of
/// <c>sec.ExternalIdentityProvisioner</c> via <see cref="ExternalIdentityProvisionerId"/>. Carries no
/// scalar columns of its own — its policy lives entirely in the ordered <see cref="Rules"/> child
/// collection (<c>sec.ClaimMappedProvisioningRule</c>), mirroring
/// <see cref="Chained.ChainedExternalIdentityProvisionerConfiguration"/>'s ordered-children shape.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner", ServiceType = "ClaimMapped")]
public sealed partial class ClaimMappedExternalIdentityProvisionerConfiguration : IExternalIdentityProvisionerImplementationConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this typed-body row
    /// (<c>sec.ClaimMappedExternalIdentityProvisioner.Id</c>).
    /// </summary>
    public Guid Id { get; set; }

    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — identified by ExternalIdentityProvisionerId */ }
    }

    string IGenericConfiguration.SectionName => "ExternalIdentityProvisioners";
    string IGenericConfiguration.ServiceType => "ExternalIdentityProvisioner";
    string? IGenericConfiguration.ServiceOptionType => "ClaimMapped";

    /// <summary>
    /// Gets or sets the durable logical FK to <c>sec.ExternalIdentityProvisioner.Id</c> (the parent header).
    /// </summary>
    public Guid ExternalIdentityProvisionerId { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordered rules this provisioner tries. Populated automatically from
    /// sec.ClaimMappedProvisioningRule child rows during configuration loading — NOT pre-sorted by
    /// the read cascade; <see cref="ClaimMappedProvisioner"/> sorts by
    /// <see cref="ClaimMappedProvisioningRuleConfiguration.ExecutionOrder"/> ascending before matching.
    /// </summary>
    public IList<ClaimMappedProvisioningRuleConfiguration> Rules { get; set; } = [];
}
