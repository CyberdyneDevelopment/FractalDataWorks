using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Commands;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>Supplies the JwtBearerAuthenticationService configuration.</summary>
public sealed class JwtBearerAuthenticationConfigurationProvider
    : ImplementationConfigurationProviderBase<IAuthenticationServiceImplementationConfiguration>,
      IJwtBearerAuthenticationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public JwtBearerAuthenticationConfigurationProvider(
        ILogger<JwtBearerAuthenticationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "JwtBearerAuthenticationService")
    {
    }
}
