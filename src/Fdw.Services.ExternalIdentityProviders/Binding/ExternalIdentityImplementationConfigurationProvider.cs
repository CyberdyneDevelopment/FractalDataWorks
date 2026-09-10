using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentity implementation's own configuration.</summary>
public sealed class ExternalIdentityImplementationConfigurationProvider
    : ImplementationProviderBase<ExternalIdentityConfiguration, IExternalIdentityImplementationConfiguration>,
      IExternalIdentityImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ExternalIdentityImplementationConfigurationProvider(
        ILogger<ExternalIdentityImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "ExternalIdentityImplementation")
    {
    }
}
