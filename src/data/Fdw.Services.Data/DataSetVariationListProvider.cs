using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Reads the list of data set variations from the <c>data.DataSetVariationList</c> view (FDW-3638).</summary>
/// <remarks>
/// The view joins a variation's data set row to its own row, so a lookup by owner, by the data set it varies, by its parent or by its name is
/// one domain-record query. The view is read only: its members are saved, read whole and retired through the data set provider, which the
/// variation provider calls.
/// </remarks>
public sealed class DataSetVariationListProvider
    : DomainConfigurationProviderBase<IDataSetVariationImplementationConfiguration, DataSetVariationDomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="DataSetVariationListProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from.</param>
    /// <param name="pathName">The schema the view lives in -- passed by its registration.</param>
    /// <param name="containerName">The view -- passed by its registration.</param>
    public DataSetVariationListProvider(
        ILogger<DataSetVariationListProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
