using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// Supplies WebMcp configuration, composing the domain record with the implementation's own.
/// </summary>
public class WebMcpConfigurationProvider
    : ImplementationConfigurationProviderBase<DomainConfiguration, IWebMcpImplementationConfiguration>,
      IWebMcpConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="WebMcpConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through.</param>
    /// <param name="pathName">The schema the WebMcp tables live in.</param>
    public WebMcpConfigurationProvider(
        ILogger<WebMcpConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "hst")
        : base(logger ?? NullLogger<WebMcpConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName,
               "WebMcp")
    {
    }
}
