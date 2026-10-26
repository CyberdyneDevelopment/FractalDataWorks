using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the GlossaryTermVersion implementation's own configuration (FDW-2331).</summary>
public sealed class GlossaryTermVersionImplementationConfigurationProvider
    : ImplementationProviderBase<GlossaryTermVersionImplementationConfiguration, IGlossaryTermVersionImplementationConfiguration>,
      IGlossaryTermVersionImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermVersionImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public GlossaryTermVersionImplementationConfigurationProvider(
        ILogger<GlossaryTermVersionImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
