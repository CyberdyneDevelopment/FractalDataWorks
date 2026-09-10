using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// Supplies ForwardedHeaders configuration, composing the domain record with the implementation's own.
/// </summary>
public class ForwardedHeadersConfigurationProvider
    : ImplementationConfigurationProviderBase<DomainConfiguration, IForwardedHeadersImplementationConfiguration>,
      IForwardedHeadersConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through.</param>
    /// <param name="pathName">The schema the ForwardedHeaders tables live in.</param>
    public ForwardedHeadersConfigurationProvider(
        ILogger<ForwardedHeadersConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "hst")
        : base(logger ?? NullLogger<ForwardedHeadersConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName,
               "ForwardedHeaders")
    {
    }
}
