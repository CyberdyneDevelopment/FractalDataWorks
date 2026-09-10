using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the DataSetAnnotation implementation's own configuration.</summary>
public sealed class DataSetAnnotationImplementationConfigurationProvider
    : ImplementationProviderBase<DataSetAnnotationImplementationConfiguration, IDataSetAnnotationImplementationConfiguration>,
      IDataSetAnnotationImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public DataSetAnnotationImplementationConfigurationProvider(
        ILogger<DataSetAnnotationImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "catalog", "DataSetAnnotationImplementation")
    {
    }
}
