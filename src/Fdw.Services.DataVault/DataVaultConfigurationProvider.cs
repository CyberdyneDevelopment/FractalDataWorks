using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.DataVault.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.DataVault;

/// <summary>Supplies the DataVault configuration.</summary>
public sealed class DataVaultConfigurationProvider
    : DomainConfigurationProviderBase<IDataVaultImplementationConfiguration>,
      IDataVaultConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataVaultConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public DataVaultConfigurationProvider(
        ILogger<DataVaultConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sec", "DataVault")
    {
    }
}
