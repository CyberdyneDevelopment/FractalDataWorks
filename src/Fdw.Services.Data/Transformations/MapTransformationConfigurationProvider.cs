using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Transformations;

/// <summary>
/// Supplies the Map implementation of a Transformation
/// (<c>transform.MapTransformationConfiguration</c>, with its field mapping rows as children).
/// </summary>
public sealed class MapTransformationConfigurationProvider
    : ImplementationProviderBase<MapTransformationConfiguration, ITransformationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MapTransformationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this implementation's rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public MapTransformationConfigurationProvider(
        ILogger<MapTransformationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
