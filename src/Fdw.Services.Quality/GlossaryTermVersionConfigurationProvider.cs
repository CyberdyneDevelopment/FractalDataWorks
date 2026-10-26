using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the GlossaryTermVersion configuration: a version whole, and a page of versions from the domain table alone (FDW-2331).</summary>
public sealed class GlossaryTermVersionConfigurationProvider
    : DomainConfigurationProviderBase<IGlossaryTermVersionImplementationConfiguration, GlossaryTermVersionDomainConfiguration>,
      IGlossaryTermVersionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="GlossaryTermVersionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public GlossaryTermVersionConfigurationProvider(
        ILogger<GlossaryTermVersionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
