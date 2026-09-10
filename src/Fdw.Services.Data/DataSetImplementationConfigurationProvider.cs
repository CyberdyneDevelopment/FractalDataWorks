using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataSet implementation's own configuration.</summary>
/// <remarks>
/// DataSet has one implementation table, so this one provider serves every DataSet kind. The
/// collection registers it into <see cref="DataSetConfigurationProvider"/> under each kind's name.
/// </remarks>
public sealed class DataSetImplementationConfigurationProvider
    : ImplementationProviderBase<DataSetImplementationConfiguration, IDataSetImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public DataSetImplementationConfigurationProvider(
        ILogger<DataSetImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "data", "DataSetImplementation")
    {
    }
}
