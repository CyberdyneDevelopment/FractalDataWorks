using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// Supplies CORS configuration, composing the domain record with the implementation's own.
/// </summary>
public class CorsConfigurationProvider
    : ServiceConfigurationProviderBase<
          CorsConfiguration,
          ICorsImplementationConfiguration,
          CorsConfigurationCommand>,
      ICorsConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="CorsConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through.</param>
    /// <param name="pathName">The schema the CORS tables live in.</param>
    public CorsConfigurationProvider(
        ILogger<CorsConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "crs")
        : base(logger ?? NullLogger<CorsConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override CorsConfiguration Compose<T>(
        string implementation,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            Implementation = implementation,
            Configuration = implementationConfiguration,
        };
}
