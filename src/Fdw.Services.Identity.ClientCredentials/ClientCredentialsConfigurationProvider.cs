using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.ClientCredentials.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>Supplies the ClientCredentialsIdentity implementation configuration.</summary>
public sealed class ClientCredentialsConfigurationProvider
    : ImplementationProviderBase<ClientCredentialsConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ClientCredentialsConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ClientCredentialsConfigurationProvider(
        ILogger<ClientCredentialsConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "ClientCredentialsIdentity")
    {
    }
}
