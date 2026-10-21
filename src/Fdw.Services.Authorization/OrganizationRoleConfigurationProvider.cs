using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the OrganizationRole configuration (FDW-1593).</summary>
public sealed class OrganizationRoleConfigurationProvider
    : DomainConfigurationProviderBase<IOrganizationRoleImplementationConfiguration>,
      IOrganizationRoleConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="OrganizationRoleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public OrganizationRoleConfigurationProvider(
        ILogger<OrganizationRoleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
