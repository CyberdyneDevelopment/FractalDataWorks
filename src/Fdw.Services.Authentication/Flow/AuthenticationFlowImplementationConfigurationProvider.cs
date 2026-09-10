using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the AuthenticationFlow implementation's own configuration.</summary>
public sealed class AuthenticationFlowImplementationConfigurationProvider
    : ImplementationProviderBase<AuthenticationFlowImplementationConfiguration, IAuthenticationFlowImplementationConfiguration>,
      IAuthenticationFlowImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public AuthenticationFlowImplementationConfigurationProvider(
        ILogger<AuthenticationFlowImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "AuthenticationFlowImplementation")
    {
    }
}
