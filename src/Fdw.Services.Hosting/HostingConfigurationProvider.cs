using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosting.Abstractions;
using Fdw.Services.Hosting.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Hosting;

/// <summary>
/// Supplies hosting configuration, composing the domain record with the implementation's own.
/// </summary>
/// <remarks>
/// Why <c>ServerConfiguration</c> rather than <c>PlatformConfiguration</c>: a hosting pipeline has to
/// exist before the platform store is reachable, so this domain's rows live in the file-backed server
/// tier declared in <c>configurationSchema.json</c>. The gateway is the ordinary one — only the
/// datastore differs.
/// </remarks>
public class HostingConfigurationProvider
    : ServiceConfigurationProviderBase<
          HostingConfiguration,
          IHostingImplementationConfiguration,
          HostingConfigurationCommand>,
      IHostingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HostingConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through — the server tier.</param>
    /// <param name="pathName">The path holding the hosting tables.</param>
    public HostingConfigurationProvider(
        ILogger<HostingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ServerConfiguration",
        string pathName = "host")
        : base(logger ?? NullLogger<HostingConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override HostingConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
