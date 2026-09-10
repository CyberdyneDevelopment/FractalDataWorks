using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>Supplies the ExternalIdentity domain configuration.</summary>
public sealed class ExternalIdentityConfigurationProvider
    : DomainConfigurationProviderBase<IExternalIdentityImplementationConfiguration>,
      IExternalIdentityConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ExternalIdentityConfigurationProvider(
        ILogger<ExternalIdentityConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "ExternalIdentity")
    {
    }
}
