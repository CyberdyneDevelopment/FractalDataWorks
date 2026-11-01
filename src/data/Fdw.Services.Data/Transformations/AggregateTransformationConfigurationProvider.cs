using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Transformations;

/// <summary>
/// Supplies the Aggregate implementation of a Transformation
/// (<c>transform.AggregateTransformationConfiguration</c>, with its group-by and aggregation rows as children).
/// </summary>
public sealed class AggregateTransformationConfigurationProvider
    : ImplementationProviderBase<AggregateTransformationConfiguration, ITransformationImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AggregateTransformationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this implementation's rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public AggregateTransformationConfigurationProvider(
        ILogger<AggregateTransformationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
