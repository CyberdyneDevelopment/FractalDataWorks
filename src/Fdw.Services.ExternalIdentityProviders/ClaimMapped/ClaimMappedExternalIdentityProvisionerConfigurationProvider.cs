using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>
/// Typed-body configuration provider for <c>sec.ClaimMappedExternalIdentityProvisioner</c> rows.
/// </summary>
/// <remarks>
/// Mirrors <c>Chained.ChainedExternalIdentityProvisionerConfigurationProvider</c> exactly. Its
/// ordered <c>Rules</c> child collection is composed automatically by the base class's
/// <c>ComposeChildren</c> cascade.
/// </remarks>
public class ClaimMappedExternalIdentityProvisionerConfigurationProvider
    : ImplementationConfigurationProvider<
          IExternalIdentityProvisionerImplementationConfiguration,
          ClaimMappedExternalIdentityProvisionerConfiguration,
          ClaimMappedExternalIdentityProvisionerConfigurationCommand>
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ClaimMappedExternalIdentityProvisionerConfigurationProvider"/> class.
    /// </summary>
    public ClaimMappedExternalIdentityProvisionerConfigurationProvider(
        ILogger<ClaimMappedExternalIdentityProvisionerConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<ClaimMappedExternalIdentityProvisionerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
