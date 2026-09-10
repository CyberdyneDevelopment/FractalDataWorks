using Fdw.Services.Configuration;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataStore implementation's own configuration.</summary>
/// <remarks>
/// DataStore has one implementation table, so this one provider serves every DataStore kind. The
/// collection registers it into <see cref="DataStoreConfigurationProvider"/> under each kind's name.
/// </remarks>
public sealed class DataStoreImplementationConfigurationProvider
    : ImplementationProviderBase<DataStoreImplementationConfiguration, IDataStoreImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public DataStoreImplementationConfigurationProvider(
        ILogger<DataStoreImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "data", "DataStoreImplementation")
    {
    }
}
