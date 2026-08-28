using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Configuration provider for IdentityServiceConfiguration rows in sec.Identity.
/// Reads through IConfigurationGateway — no IConfiguration binding section.
/// </summary>
public class IdentityServiceConfigurationProvider
    : ServiceConfigurationProviderBase<
          IdentityServiceConfiguration,
          IIdentityServiceImplementationConfiguration,
          IdentityServiceConfigurationCommand>,
      IIdentityServiceConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="IdentityServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the named connection.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema holding the configuration.</param>
    public IdentityServiceConfigurationProvider(
        ILogger<IdentityServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "sec")
        : base(logger ?? NullLogger<IdentityServiceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override IdentityServiceConfiguration Compose<T>(
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
