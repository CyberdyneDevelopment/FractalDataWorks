using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the DataView implementation of a data set variation (<c>data.DataViewDataSetVariationConfiguration</c>).</summary>
/// <remarks>
/// Registered into <see cref="DataSetVariationConfigurationProvider"/> under
/// <see cref="DataViewDataSetVariationConfiguration.ImplementationName"/>.
/// </remarks>
public sealed class DataViewDataSetVariationConfigurationProvider
    : ImplementationProviderBase<DataViewDataSetVariationConfiguration, IDataSetVariationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataViewDataSetVariationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public DataViewDataSetVariationConfigurationProvider(
        ILogger<DataViewDataSetVariationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
