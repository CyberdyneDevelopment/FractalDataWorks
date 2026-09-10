using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Hosts;

/// <summary>
/// Supplies ApiReference configuration, composing the domain record with the implementation's own.
/// </summary>
public class ApiReferenceConfigurationProvider
    : ImplementationConfigurationProviderBase<ApiReferenceConfiguration, IApiReferenceImplementationConfiguration, ApiReferenceConfigurationCommand>,
      IApiReferenceConfigurationProvider
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ApiReferenceConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through.</param>
    /// <param name="pathName">The schema the ApiReference tables live in.</param>
    public ApiReferenceConfigurationProvider(
        ILogger<ApiReferenceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "hst")
        : base(logger ?? NullLogger<ApiReferenceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
