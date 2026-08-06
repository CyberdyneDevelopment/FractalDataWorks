using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    /// Phase 2: Initializes by registering factories with the EtlRowMapperProvider.
    /// Call after Build().
    /// </summary>
    public static void Initialize(IServiceProvider services, ILoggerFactory? loggerFactory = null)
    {
        var provider = services.GetRequiredService<IEtlRowMapperProvider>();

        foreach (var type in All())
        {
            type.RegisterFactory(provider, services);
        }

        if (provider is EtlRowMapperProvider concreteProvider)
        {
            concreteProvider.CompleteInitialization();
        }

        if (loggerFactory != null)
        {
            var logger = loggerFactory.CreateLogger<EtlRowMapperTypes>();
            var mapperCount = All().Count;
            EtlRowMapperLog.ProviderInitialized(logger, mapperCount);
        }
    }
}
