using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>Reads which role names carry system authority.</summary>
/// <remarks>
/// Closed over the RoleMapping domain's implementation contract, because that is what the domain
/// provider's registry accepts: <c>Register&lt;T&gt;</c> constrains T to
/// <c>IImplementationConfigurationProvider</c> of the domain contract, so a provider closed over
/// anything narrower cannot be registered.
/// </remarks>
public class SystemRoleMappingConfigurationProvider
    : ImplementationConfigurationProvider<
          IRoleMappingImplementationConfiguration,
          SystemRoleMappingConfiguration,
          SystemRoleMappingConfigurationCommand>
{

    /// <summary>Initializes a new instance of the <see cref="SystemRoleMappingConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the store these rows live on.</param>
    /// <param name="dataStoreName">The store the domain's rows live in.</param>
    /// <param name="pathName">The path the rows live under.</param>
    public SystemRoleMappingConfigurationProvider(
        ILogger<ImplementationConfigurationProviderBase<SystemRoleMappingConfiguration, ISystemRoleMappingImplementationConfiguration, SystemRoleMappingConfigurationCommand>>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName)
        : base(logger ?? NullLogger<ImplementationConfigurationProviderBase<SystemRoleMappingConfiguration, ISystemRoleMappingImplementationConfiguration, SystemRoleMappingConfigurationCommand>>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
