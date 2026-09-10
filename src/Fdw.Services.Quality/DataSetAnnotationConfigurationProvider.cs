using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured DataSetAnnotation members.</summary>
public sealed class DataSetAnnotationConfigurationProvider
    : DomainConfigurationProviderBase<IDataSetAnnotationImplementationConfiguration>,
      IDataSetAnnotationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public DataSetAnnotationConfigurationProvider(
        ILogger<DataSetAnnotationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "catalog", "DataSetAnnotation")
    {
    }
}
