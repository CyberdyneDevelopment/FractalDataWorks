using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.DataVault;

/// <summary>Supplies the DataVault domain configuration.</summary>
public sealed class DataVaultConfigurationProvider
    : DomainConfigurationProviderBase<IDataVaultImplementationConfiguration>,
      IDataVaultConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataVaultConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public DataVaultConfigurationProvider(
        ILogger<DataVaultConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "DataVault")
    {
    }
}
