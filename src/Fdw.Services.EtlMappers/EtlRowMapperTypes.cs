using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.EtlMappers;

/// <summary>
/// Collection of ETL row mapper types with Configure/Register/Initialize pattern.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(
    typeof(EtlRowMapperTypeBase<IEtlRowMapper, IEtlRowMapperFactory<IEtlRowMapper, EtlRowMapperConfiguration>, EtlRowMapperConfiguration>),
    typeof(IEtlRowMapperType),
    typeof(EtlRowMapperTypes))]
public partial class EtlRowMapperTypes : TypeCollectionBase<
    EtlRowMapperTypeBase<IEtlRowMapper, IEtlRowMapperFactory<IEtlRowMapper, EtlRowMapperConfiguration>, EtlRowMapperConfiguration>,
    IEtlRowMapperType>
{
    /// <summary>
    /// Phase 1a: Configures IOptions bindings for all mapper types.
    /// Call before Build().
    /// </summary>
    public static void Configure(IServiceCollection services, IConfiguration configuration, ILoggerFactory? loggerFactory = null)
    {
        foreach (var type in All())
        {
            type.Configure(services, configuration, loggerFactory);
        }
    }

    /// <summary>
    /// Phase 1b: Registers required services (factories) for all mapper types.
    /// Call before Build().
    /// </summary>
    public static void Register(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {
        // Register the provider
        services.AddSingleton<IEtlRowMapperProvider, EtlRowMapperProvider>();

        // Register each type's required services
        foreach (var type in All())
        {
            type.Register(services, loggerFactory);
        }
    }

    /// <summary>
    /// Phase 3: fills the <see cref="IEtlRowMapperProvider"/> with each mapper type's factory.
    /// Call after Build().
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <returns>The host, for chaining.</returns>
    /// <remarks>
    /// Why the provider is filled from here rather than each option pushing itself in: an option knows
    /// its factory TYPE (<see cref="IEtlRowMapperType.FactoryType"/>) and its name, and the container
    /// knows how to build that type — so the mapping the provider needs is recoverable without a
    /// per-option <c>RegisterFactory</c> member existing purely to be called by a loop here. The service
    /// that needs the factories resolved is the one resolving them.
    /// </remarks>
    public static IHost Initialize(IHost host, ILoggerFactory? loggerFactory = null)
    {
        var services = host.Services;
        var provider = services.GetRequiredService<IEtlRowMapperProvider>();

        foreach (var type in All())
        {
            // GetRequiredService, not GetService: the option's own Register put this factory in the
            // container, so its absence is a broken container, not an optional feature — and a missing
            // factory would otherwise surface as a mapper that silently cannot be created.
            provider.Register(type.Name, (IEtlRowMapperFactory)services.GetRequiredService(type.FactoryType));
        }

        if (provider is EtlRowMapperProvider concreteProvider)
        {
            concreteProvider.CompleteInitialization();
        }

        if (loggerFactory != null)
        {
            EtlRowMapperLog.ProviderInitialized(loggerFactory.CreateLogger<EtlRowMapperTypes>(), All().Count);
        }

        return host;
    }
}
