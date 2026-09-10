using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity;

/// <summary>Supplies the Identity configuration.</summary>
public sealed class IdentityServiceConfigurationProvider
    : DomainConfigurationProviderBase<IIdentityServiceImplementationConfiguration>,
      IIdentityServiceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="IdentityServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public IdentityServiceConfigurationProvider(
        ILogger<IdentityServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sec", "Identity")
    {
    }
}
