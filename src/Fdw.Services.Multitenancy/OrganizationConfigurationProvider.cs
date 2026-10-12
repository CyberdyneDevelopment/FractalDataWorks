using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy;

/// <summary>Supplies the configured organizations, read from <c>tenant.Organization</c> through the configuration gateway.</summary>
public sealed class OrganizationConfigurationProvider
    : DomainConfigurationProviderBase<IOrganizationImplementationConfiguration, OrganizationDomainConfiguration>,
      IOrganizationConfigurationProvider
{
    /// <summary>Initializes the provider.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto <paramref name="dataStoreName"/>.</param>
    /// <param name="dataStoreName">The configuration connection the rows live on.</param>
    /// <param name="pathName">The schema the rows live in.</param>
    /// <param name="containerName">The domain table.</param>
    public OrganizationConfigurationProvider(
        ILogger<OrganizationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
    }
}
