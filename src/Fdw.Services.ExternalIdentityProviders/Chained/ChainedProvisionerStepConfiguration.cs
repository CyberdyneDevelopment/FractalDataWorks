using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>
/// Ordered child of <see cref="ChainedExternalIdentityProvisionerConfiguration"/> — one row per
/// sibling <c>sec.ExternalIdentityProvisioner</c> the chain delegates <c>Provision</c> to, in
/// <see cref="ExecutionOrder"/>. Mirrors <c>Fdw.Operations.Configuration.EscalationLevelConfiguration</c>'s
/// ordered-child shape: the read cascade (<c>DefaultConfigurationProvider.ComposeChildren</c>) does NOT
/// apply an ORDER BY, so <see cref="Chained.ChainedExternalIdentityProvisioner"/> sorts
/// <see cref="ExecutionOrder"/> ascending itself before walking the steps.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisioner")]
public sealed partial class ChainedProvisionerStepConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    // Why: ServiceCategory matches the MsSqlConfigurationSource key generation convention.
    public string SectionName => "ExternalIdentityProvisioners";

    /// <inheritdoc />
    // Why: matches ServiceCategory from [ManagedConfiguration] for IOptions binding path.
    public string ServiceType => "ExternalIdentityProvisioner";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the display name for this step.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this step.
    /// </summary>
    // Why: no default GUID — child Ids are minted by the save cascade on insert when empty (the single
    // sanctioned place). Pre-minting here is a forbidden default-GUID fallback.
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the owning ChainedExternalIdentityProvisioner (logical FK to
    /// sec.ChainedExternalIdentityProvisioner.Id).
    /// </summary>
    public Guid ChainedExternalIdentityProvisionerId { get; set; }

    /// <summary>
    /// Gets or sets the name of the sibling <c>sec.ExternalIdentityProvisioner</c> this step delegates
    /// <c>Provision</c> to. Resolved by name through the injected
    /// <c>IPlatformServiceProvider&lt;IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration&gt;</c>
    /// at runtime — never a hard FK, never a switch.
    /// </summary>
    public string ProvisionerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ascending order in which this step is tried within its owning chain.
    /// </summary>
    public int ExecutionOrder { get; set; }
}
