using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Supplies role-mapping configuration, composing the domain record with the implementation's own.
/// </summary>
public class RoleMappingConfigurationProvider
    : ServiceConfigurationProviderBase<
          RoleMappingConfiguration,
          IRoleMappingImplementationConfiguration,
          RoleMappingConfigurationCommand>,
      IRoleMappingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoleMappingConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Yields the gateway for the named datastore.</param>
    /// <param name="dataStoreName">The datastore this reads through.</param>
    /// <param name="pathName">The path holding the role-mapping tables.</param>
    public RoleMappingConfigurationProvider(
        ILogger<RoleMappingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "authz")
        : base(logger ?? NullLogger<RoleMappingConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override RoleMappingConfiguration Compose<T>(
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
