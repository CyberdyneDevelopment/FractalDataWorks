using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Limits;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Default DataGateway service type that registers <see cref="IDataGateway"/>,
/// <see cref="IDataStoreProvider"/>, and <see cref="ISchemaInformationService"/>
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(DataGatewayTypes), "Default")]
public sealed class DefaultDataGatewayServiceType : DataGatewayTypeBase<IGenericService, IDataGatewayFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDataGatewayServiceType"/> class.
    /// </summary>
    public DefaultDataGatewayServiceType()
        : base(
            "Default",
            "DataGateway:Default",
            "Default DataGateway",
            "Default DataGateway with DataStoreProvider, SchemaInformation, and DataSetResolver")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            // Why: IDataStoreProvider/ConfigurationGatewayDataStoreProvider registration is NOT duplicated
            // here — it is owned exclusively by ConfigurationGatewayDataStoreProvider.Register(), called
            // explicitly by every reference app's Program.cs. A prior
            // TryAddSingleton<IDataStoreProvider, ConfigurationGatewayDataStoreProvider>() line here was a
            // legacy duplicate registration left over from before ConfigurationGatewayDataStoreProvider
            // became Scoped (owner ruling 2026-07-02, tenant-scoped datastore visibility). It was harmless
            // only by accident of registration order: reference-api calls DataGatewayTypes.Register() (this
            // method) BEFORE ConfigurationGatewayDataStoreProvider.Register(), so the TryAddSingleton here
            // won the race and left a stale, never-collected Singleton ServiceDescriptor for
            // IDataStoreProvider in the container (shadowed at resolution time by
            // ConfigurationGatewayDataStoreProvider.Register()'s later Add-registration, but still present
            // for container-wide validation). With ConfigurationGatewayDataStoreProvider now Scoped, that
            // stale Singleton descriptor is a captive-dependency violation in its own right and would fail
            // ValidateOnBuild/ValidateScopes regardless of which registration ultimately wins. Deleting the
            // duplicate here — not touching any app's Program.cs — removes the hazard at its source.
            // Why: Lazy<IDataSetConfigurationProvider> breaks the circular dependency:
            // IDataSetConfigurationProvider (DataSetProvider) → IDataGateway
            // → (Lazy) IDataSetConfigurationProvider. DataGatewayService uses the config provider
            // (not the runtime IDataSetProvider) because it needs DataSetConfiguration records.
            builder.Services.TryAddSingleton(sp => new Lazy<IDataSetConfigurationProvider>(() => sp.GetRequiredService<IDataSetConfigurationProvider>()));
            builder.Services.TryAddSingleton<IDataGateway, DataGatewayService>();
            // Why: ISchemaInformationService replaces ISchemaDiscoveryOrchestrator — cache-first,
            // demand-driven discovery eliminates the startup race and the orchestration complexity.
            builder.Services.TryAddScoped<ISchemaInformationService, SchemaInformationService>();

            // Why: IMemoryCache is the backing store for DataGatewayResultCache — register it here
            // so both test hosts and production servers have it available.
            builder.Services.AddMemoryCache();

            // Why: DataGatewayService is SCOPED — it selects connections via the scoped IConnectionProvider
            // and reads per-request auth context. Caching is now built in (P3), so no decorator is needed;
            // DataGatewayService is the single class that reads from the process-wide singleton cache.
            builder.Services.TryAddScoped<DataGatewayService>();

            // Why: Cache STATE is process-wide — DataGatewayResultCache is a singleton store the scoped
            // DataGatewayService consults. ICacheInvalidator (domain providers' write paths) points at the
            // same store so invalidation hits the shared cache without any additional wiring.
            builder.Services.TryAddSingleton<DataGatewayResultCache>();
            builder.Services.AddSingleton<ICacheInvalidator>(sp => sp.GetRequiredService<DataGatewayResultCache>());

            // Why: Replace any prior IDataGateway registration; the real one is the scoped decorator chain below.
            var existing = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IDataGateway));
            if (existing != null)
                builder.Services.Remove(existing);

            // Why: PassThroughConnectionLimitResolver is the default (no-op) resolver; replace it with a
            // configuration-backed resolver via builder.Services.AddSingleton<IConnectionLimitResolver, ...>() in
            // Program.cs before DataGatewayTypes.Register() if limits are needed.
            builder.Services.TryAddSingleton<IConnectionLimitResolver, PassThroughConnectionLimitResolver>();
            builder.Services.TryAddSingleton<ConnectionLimitCounterStore>();

            // Why: LimitEnforcementDataGateway is the outermost decorator (limits checked before dispatch).
            // It wraps DataGatewayService directly — CachingDataGateway is deleted; caching is now built into
            // DataGatewayService. Scoped to match the chain; counters live in the singleton store.
            builder.Services.AddScoped<LimitEnforcementDataGateway>(sp =>
                new LimitEnforcementDataGateway(
                    sp.GetRequiredService<DataGatewayService>(),
                    sp.GetRequiredService<IConnectionLimitResolver>(),
                    sp.GetRequiredService<ConnectionLimitCounterStore>(),
                    sp.GetService<ILoggerFactory>()));

            builder.Services.AddScoped<IDataGateway>(sp => sp.GetRequiredService<LimitEnforcementDataGateway>());

            // Why: DailyLimitResetJob resets in-memory daily counters at midnight UTC.
            // Registered as a hosted service so it starts automatically with the host.
            builder.Services.AddHostedService(sp =>
                new DailyLimitResetJob(
                    sp.GetRequiredService<ConnectionLimitCounterStore>(),
                    sp.GetService<ILoggerFactory>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
    
        });

        Configuration(builder =>
        {

            // Why: Bind EnableCache from the "DataGateway" section so etl/scheduler can set
            // DataGateway:EnableCache=false in their appsettings to bypass the result cache entirely,
            // eliminating cross-process staleness without any per-call opt-out.
            builder.Services.Configure<DataGatewayOptions>(builder.Configuration.GetSection("DataGateway"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

    }

}
