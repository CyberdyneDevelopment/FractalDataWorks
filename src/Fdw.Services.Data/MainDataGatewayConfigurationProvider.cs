using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>Reads how the data gateway behaves.</summary>
public class MainDataGatewayConfigurationProvider
    : ImplementationConfigurationProvider<
          IDataGatewayImplementationConfiguration,
          MainDataGatewayConfiguration,
          MainDataGatewayConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="MainDataGatewayConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the server tier.</param>
    /// <param name="dataStoreName">The store this row lives in.</param>
    /// <param name="pathName">The path the row lives under.</param>
    public MainDataGatewayConfigurationProvider(
        ILogger<MainDataGatewayConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "settings")
        : base(logger ?? NullLogger<MainDataGatewayConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
