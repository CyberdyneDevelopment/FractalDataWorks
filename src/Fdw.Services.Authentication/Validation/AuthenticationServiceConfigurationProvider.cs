using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Commands;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>Supplies the AuthenticationService configuration.</summary>
public sealed class AuthenticationServiceConfigurationProvider
    : DomainConfigurationProviderBase<IAuthenticationServiceImplementationConfiguration>,
      IAuthenticationServiceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public AuthenticationServiceConfigurationProvider(
        ILogger<AuthenticationServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "AuthenticationService")
    {
    }
}
