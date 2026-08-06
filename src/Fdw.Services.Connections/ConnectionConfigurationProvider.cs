using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Commands;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Connections;

/// <summary>
/// Domain-specific configuration provider for connections.
/// The polymorphic typed-body read (dispatch on <c>ServiceOptionType</c> to load the typed body row,
/// e.g. <c>conn.MsSqlConnection</c>, and attach it to <see cref="ConnectionConfiguration.Configuration"/>)
/// is composed uniformly by <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>; typed providers
/// are registered via the inherited <c>RegisterTypedProvider</c>.
/// </summary>
public class ConnectionConfigurationProvider : DefaultConfigurationProvider<ConnectionConfiguration, ConnectionConfigurationCommand>
{
    /// <summary>
    /// Registers the ConnectionConfigurationProvider with DI, targeting this domain's own default
    /// location (this class's own constructor default). To route Connection configuration to a
    /// non-default store/path, call <see cref="DefaultConfigurationProvider{TConfig,TCommand}.SetConfiguration"/>
    /// on the resolved singleton — never register a second time with different arguments.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<ConnectionConfigurationProvider>(sp =>
            new ConnectionConfigurationProvider(
                sp.GetService<ILogger<ConnectionConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        services.TryAddSingleton<DefaultConfigurationProvider<ConnectionConfiguration, ConnectionConfigurationCommand>>(
            sp => sp.GetRequiredService<ConnectionConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<ConnectionConfiguration>>(
            sp => sp.GetRequiredService<ConnectionConfigurationProvider>());

        // Why: the Connections domain contributes ONE domain-level IHealthCheckable that enumerates
        // conn.Connection rows at CHECK TIME (rows are runtime data, never per-row DI registrations) —
        // registered here, inside the same cascade every connection-kind option already calls, rather
        // than as a separate line in an application's Program.cs (RegisterDomainConfiguration cascade rule).
        // TryAddEnumerable dedups by implementation type, so every connection-kind option calling this
        // method stays idempotent — only one ConnectionsHealthCheckable is ever added.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthCheckable, ConnectionsHealthCheckable>());

        // Why Scoped: ConnectionHealthService depends on the Scoped IDataGateway — a Singleton
        // registration would pin the first-resolved scope's gateway forever.
        services.TryAddScoped<IConnectionHealthService, ConnectionHealthService>();

        // Why the manual Any() guard instead of TryAddEnumerable/AddHostedService: IHostedService
        // registrations are not deduplicated by AddHostedService, and this method is called once per
        // registered connection-kind option (same cascade as ConnectionsHealthCheckable above) — an
        // explicit implementation-type check keeps exactly one worker registered regardless of how
        // many connection types call RegisterDomainConfiguration.
        if (!services.Any(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(ConnectionHealthMonitorWorker)))
        {
            services.AddHostedService<ConnectionHealthMonitorWorker>();
        }

        // Why: forwards IDataConnectionProvider to the generated IConnectionProvider (DefaultConnectionProvider
        // implements both) — an alias never outlives what it aliases, and IConnectionProvider is generated
        // Scoped (GenerateProvider=true), so the alias is Scoped to MATCH it. A Singleton alias over a
        // Scoped service would be a captive dependency, throwing under Development ValidateScopes. A domain
        // that needs different lifetimes overrides the Register phase rather than declaring a lifetime on
        // the collection — which is why no lifetime knob exists on the attribute. Replaces the old,
        // separately-called
        // ConnectionTypes.RegisterAdditionalInterfaces() — folded into this same idempotent cascade every
        // connection-kind option already calls, so no host Program.cs needs its own call for it.
        services.TryAddScoped<IDataConnectionProvider>(sp =>
            (IDataConnectionProvider)sp.GetRequiredService<IConnectionProvider>());

        // Why: IServiceConnectionProvider serves framework-internal connections (e.g. ConfigurationDb).
        services.TryAddSingleton<IServiceConnectionProvider, DefaultServiceConnectionProvider>();
    }

    /// <summary>Initializes a new instance of the <see cref="ConnectionConfigurationProvider"/> class.</summary>
    public ConnectionConfigurationProvider(
        ILogger<ConnectionConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "conn",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<ConnectionConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
