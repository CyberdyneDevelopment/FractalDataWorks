using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>
/// Reads and writes <see cref="SavedViewConfiguration"/>.
/// </summary>
/// <remarks>
/// A saved view is its own root rather than a child of a dataverse, because lineage points at it
/// and one view can serve several projects. Membership is expressed by attaching it as a dataverse
/// resource, not by owning it here.
/// </remarks>
public class SavedViewConfigurationProvider
    : ImplementationConfigurationProviderBase<ISavedViewImplementationConfiguration>
{

    /// <summary>
    /// Registers the provider and the interfaces callers resolve it through.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<SavedViewConfigurationProvider>(sp =>
            new SavedViewConfigurationProvider(
                sp.GetService<ILogger<SavedViewConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection,
                "dataverse"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<ISavedViewImplementationConfiguration>>(
            sp => sp.GetRequiredService<SavedViewConfigurationProvider>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SavedViewConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger, or null for a functional provider without logging.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema the dataverse tables live in.</param>
    public SavedViewConfigurationProvider(
        ILogger<SavedViewConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "dataverse")
        : base(logger ?? NullLogger<SavedViewConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName, "SavedView")
    {
    }
}
