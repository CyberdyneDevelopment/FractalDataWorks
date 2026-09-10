using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.JwtAssertion.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>Supplies the JwtAssertionIdentity configuration.</summary>
public sealed class JwtAssertionConfigurationProvider
    : ImplementationProviderBase<JwtAssertionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtAssertionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public JwtAssertionConfigurationProvider(
        ILogger<JwtAssertionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "JwtAssertionIdentity")
    {
    }
}
