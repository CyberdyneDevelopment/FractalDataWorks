using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.SecretManagers.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers;

/// <summary>Supplies the SecretManager configuration.</summary>
public sealed class SecretManagerConfigurationProvider
    : ImplementationConfigurationProviderBase<ISecretManagerImplementationConfiguration>,
      ISecretManagerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SecretManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public SecretManagerConfigurationProvider(
        ILogger<SecretManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "SecretManager")
    {
    }
}
