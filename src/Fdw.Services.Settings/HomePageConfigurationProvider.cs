using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the HomePage domain (FDW-2325).</summary>
public sealed class HomePageConfigurationProvider
    : DomainConfigurationProviderBase<IHomePageImplementationConfiguration>,
      IHomePageConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HomePageConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The data store the domain lives in.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="containerName">The container of the domain row.</param>
    public HomePageConfigurationProvider(
        ILogger<HomePageConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
