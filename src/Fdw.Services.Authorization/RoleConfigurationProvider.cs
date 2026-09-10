using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Role configuration.</summary>
public sealed class RoleConfigurationProvider
    : ImplementationConfigurationProviderBase<IRoleImplementationConfiguration>,
      IRoleConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RoleConfigurationProvider(
        ILogger<RoleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "Role")
    {
    }
}
