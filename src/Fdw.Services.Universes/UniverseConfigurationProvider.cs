using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Universes.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Universes;

/// <summary>
/// Reads and writes <see cref="UniverseConfiguration"/> and its children.
/// </summary>
/// <remarks>
/// There are no Get overrides here. The base composes the aggregate — members, resources and
/// relationships come back populated — because those are direct children of the universe row.
/// <c>DataSetConfigurationProvider</c> overrides Get only to reach a grandchild, which a universe
/// does not have.
/// </remarks>
public class UniverseConfigurationProvider
    : ImplementationConfigurationProviderBase<UniverseConfiguration, UniverseConfigurationCommand>,
      IUniverseConfigurationProvider
{
    /// <summary>
    /// Registers the provider and the interfaces callers resolve it through.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<UniverseConfigurationProvider>(sp =>
            new UniverseConfigurationProvider(
                sp.GetService<ILogger<UniverseConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection,
                "universe"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<UniverseConfiguration, UniverseConfigurationCommand>>(
            sp => sp.GetRequiredService<UniverseConfigurationProvider>());

        services.TryAddSingleton<IUniverseConfigurationProvider>(
            sp => sp.GetRequiredService<UniverseConfigurationProvider>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniverseConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger, or null for a functional provider without logging.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema the universe tables live in.</param>
    public UniverseConfigurationProvider(
        ILogger<UniverseConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "universe")
        : base(logger ?? NullLogger<UniverseConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
