using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the home page implementation of the HomePage domain (FDW-2325).</summary>
public sealed class HomePageImplementationConfigurationProvider
    : ImplementationProviderBase<HomePageImplementationConfiguration, IHomePageImplementationConfiguration>,
      IHomePageImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HomePageImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The data store the implementation lives in.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="containerName">The container of the implementation row.</param>
    public HomePageImplementationConfigurationProvider(
        ILogger<HomePageImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
