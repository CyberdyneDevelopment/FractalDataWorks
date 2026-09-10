using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentityProvisionerBinding implementation's own configuration.</summary>
public sealed class ExternalIdentityProvisionerBindingImplementationConfigurationProvider
    : ImplementationProviderBase<ExternalIdentityProvisionerBindingImplementationConfiguration, IExternalIdentityProvisionerBindingImplementationConfiguration>,
      IExternalIdentityProvisionerBindingImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerBindingImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ExternalIdentityProvisionerBindingImplementationConfigurationProvider(
        ILogger<ExternalIdentityProvisionerBindingImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "ExternalIdentityProvisionerBindingImplementation")
    {
    }
}
