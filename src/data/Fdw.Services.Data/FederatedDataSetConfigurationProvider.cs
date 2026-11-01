using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the Federated implementation of DataSet (<c>data.FederatedDataSetConfiguration</c>).</summary>
/// <remarks>
/// Registered into <see cref="DataSetConfigurationProvider"/> under
/// <see cref="FederatedDataSetConfiguration.ImplementationName"/>. Each kind of data set has its own
/// implementation table and its own provider (FDW-1031).
/// </remarks>
public sealed class FederatedDataSetConfigurationProvider
    : ImplementationProviderBase<FederatedDataSetConfiguration, IDataSetImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FederatedDataSetConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public FederatedDataSetConfigurationProvider(
        ILogger<FederatedDataSetConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
