using Fdw.Services.Quality.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the DataSetAnnotation configuration.</summary>
public sealed class DataSetAnnotationConfigurationProvider
    : DomainConfigurationProviderBase<IDataSetAnnotationImplementationConfiguration>,
      IDataSetAnnotationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public DataSetAnnotationConfigurationProvider(
        ILogger<DataSetAnnotationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "catalog", "DataSetAnnotation")
    {
    }
}
