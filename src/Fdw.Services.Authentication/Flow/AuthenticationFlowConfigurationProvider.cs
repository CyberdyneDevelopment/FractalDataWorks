using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the AuthenticationFlow domain configuration.</summary>
public sealed class AuthenticationFlowConfigurationProvider
    : DomainConfigurationProviderBase<IAuthenticationFlowImplementationConfiguration>,
      IAuthenticationFlowConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public AuthenticationFlowConfigurationProvider(
        ILogger<AuthenticationFlowConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "AuthenticationFlow")
    {
    }
}
