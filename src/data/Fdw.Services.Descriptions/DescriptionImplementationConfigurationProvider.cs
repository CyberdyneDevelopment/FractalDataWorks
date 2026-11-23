using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Descriptions;

/// <summary>Supplies the description implementation's own configuration, read from <c>catalog.DescriptionConfiguration</c>.</summary>
public sealed class DescriptionImplementationConfigurationProvider
    : ImplementationProviderBase<DescriptionImplementationConfiguration, IDescriptionImplementationConfiguration>,
      IDescriptionImplementationConfigurationProvider
{
    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The implementation table.</param>
    public DescriptionImplementationConfigurationProvider(
        ILogger<DescriptionImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
