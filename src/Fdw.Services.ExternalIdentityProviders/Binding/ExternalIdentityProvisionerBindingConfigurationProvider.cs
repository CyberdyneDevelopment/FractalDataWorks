using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentityProvisionerBinding configuration.</summary>
public sealed class ExternalIdentityProvisionerBindingConfigurationProvider
    : DomainConfigurationProviderBase<IExternalIdentityProvisionerBindingImplementationConfiguration>,
      IExternalIdentityProvisionerBindingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerBindingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ExternalIdentityProvisionerBindingConfigurationProvider(
        ILogger<ExternalIdentityProvisionerBindingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "ExternalIdentityProvisionerBinding")
    {
    }
}
