using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Permission implementation's own configuration.</summary>
public sealed class PermissionImplementationConfigurationProvider
    : ImplementationProviderBase<PermissionImplementationConfiguration, IPermissionImplementationConfiguration>,
      IPermissionImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="PermissionImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public PermissionImplementationConfigurationProvider(
        ILogger<PermissionImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "authz", "PermissionImplementation")
    {
    }
}
