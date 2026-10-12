using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Transformations;

/// <summary>
/// Supplies the Filter implementation of a Transformation
/// (<c>transform.FilterTransformationConfiguration</c>, which has no child rows).
/// </summary>
public sealed class FilterTransformationConfigurationProvider
    : ImplementationProviderBase<FilterTransformationConfiguration, ITransformationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="FilterTransformationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this implementation's rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public FilterTransformationConfigurationProvider(
        ILogger<FilterTransformationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
