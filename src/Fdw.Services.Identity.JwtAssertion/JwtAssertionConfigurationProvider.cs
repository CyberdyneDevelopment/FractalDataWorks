using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.JwtAssertion.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>Supplies the JwtAssertionIdentity configuration.</summary>
public sealed class JwtAssertionConfigurationProvider
    : ImplementationProviderBase<JwtAssertionConfiguration, IIdentityServiceImplementationConfiguration>,
      IJwtAssertionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="JwtAssertionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public JwtAssertionConfigurationProvider(
        ILogger<JwtAssertionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sec", "JwtAssertionIdentity")
    {
    }
}
