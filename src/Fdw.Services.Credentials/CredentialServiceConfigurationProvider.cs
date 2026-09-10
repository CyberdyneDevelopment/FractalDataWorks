using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Credentials.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Credentials;

/// <summary>Supplies the CredentialService configuration.</summary>
public sealed class CredentialServiceConfigurationProvider
    : DomainConfigurationProviderBase<ICredentialServiceImplementationConfiguration>,
      ICredentialServiceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="CredentialServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public CredentialServiceConfigurationProvider(
        ILogger<CredentialServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "CredentialService")
    {
    }
}
