using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

using Fdw.Services.ExternalIdentityProviders.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>
/// Flat selector row binding a (<see cref="TenantId"/>, <see cref="ProviderName"/>) pair to the named
/// <c>sec.ExternalIdentityProvisioner</c> that should handle first-login provisioning for that external
/// identity provider, for that tenant. Persisted to <c>sec.ExternalIdentityProvisionerBinding</c>. A
/// root configuration — no typed body, no <c>ServiceOptionType</c> discriminator.
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> null means the global/system binding. Matching is EXACT (TenantId, ProviderName)
/// equality, including null==null for the global row — there is NO tenant-to-global fall-through (see
/// <see cref="ExternalIdentityProvisionerBindingConfigurationProvider.ResolveProvisionerName"/>). An
/// absent binding is Success(null), meaning provisioning stays default-OFF for that provider; more than
/// one current match for the same pair is a fail-loud ambiguity, never a silent "first match wins".
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "ExternalIdentityProvisionerBinding")]
public sealed partial class ExternalIdentityProvisionerBindingConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "ExternalIdentityProvisionerBindings";

    /// <inheritdoc />
    public string ServiceType => "ExternalIdentityProvisionerBinding";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the durable logical identity across versions.
    /// No default — the database assigns identity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable label for this binding.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant this binding applies to. Null means the global/system binding for
    /// <see cref="ProviderName"/>.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the external identity provider configuration
    /// (<c>auth.ExternalIdentityProvider.Name</c>) this binding selects a provisioner for.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the <c>sec.ExternalIdentityProvisioner</c> to invoke for first-login
    /// provisioning of the matched (<see cref="TenantId"/>, <see cref="ProviderName"/>) pair.
    /// </summary>
    public string ProvisionerName { get; set; } = string.Empty;
}
