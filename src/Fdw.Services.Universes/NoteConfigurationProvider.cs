using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Universes.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Universes;

/// <summary>Reads and writes the notes raised against a universe.</summary>
public class NoteConfigurationProvider
    : ImplementationConfigurationProviderBase<NoteConfiguration, NoteConfigurationCommand>
{
    /// <summary>Registers this provider and the base it is resolved through.</summary>
    /// <param name="services">The service collection to register into.</param>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<NoteConfigurationProvider>(sp =>
            new NoteConfigurationProvider(
                sp.GetService<ILogger<NoteConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection,
                "universe"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<NoteConfiguration, NoteConfigurationCommand>>(
            sp => sp.GetRequiredService<NoteConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="NoteConfigurationProvider"/> class.</summary>
    public NoteConfigurationProvider(
        ILogger<NoteConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "universe")
        : base(logger ?? NullLogger<NoteConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
