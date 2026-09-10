using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Flow;

/// <summary>Supplies the steps a flow runs, in order.</summary>
public sealed class AuthenticationFlowStepConfigurationProvider
    : ImplementationProviderBase<AuthenticationFlowStepImplementationConfiguration, IAuthenticationFlowStepImplementationConfiguration>,
      IAuthenticationFlowStepConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowStepConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public AuthenticationFlowStepConfigurationProvider(
        ILogger<AuthenticationFlowStepConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "AuthenticationFlowStep")
    {
    }
}
