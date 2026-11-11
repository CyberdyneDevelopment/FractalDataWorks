using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Caching;
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Data;

/// <summary>
/// Collection of configuration gateway service types — the gateways that read and write
/// configuration itself.
/// </summary>
/// <remarks>
/// Separate from <see cref="DataGatewayServiceTypes"/> because the two answer different questions. A data
/// gateway reaches a DataStore that configuration describes; a configuration gateway reaches the
/// store configuration lives in, and is bound from <c>configurationSchema.json</c> before any row is
/// readable. One collection could carry only one of those.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(ConfigurationGatewayTypeBase<IGenericService, IConfigurationGatewayFactory>),
    typeof(IConfigurationGatewayType),
    typeof(ConfigurationGatewayTypes),
    ServiceCategory = "ConfigurationGateway")]
public partial class ConfigurationGatewayTypes : ServiceTypeCollectionBase<
    ConfigurationGatewayTypeBase<IGenericService, IConfigurationGatewayFactory>,
    IConfigurationGatewayType>
{
    /// <summary>
    /// The schema file declaring the connections this app opens before any configuration is readable.
    /// </summary>
    public static string SchemaFileName { get; set; } = "configurationSchema.json";

    /// <summary>
    /// The most keys one statement carries when configuration rows are read by the keys of the rows above them (FDW-4254), and so the
    /// size of the chunks a larger set is read in. Every reader takes it from <see cref="IConfigurationGatewayProvider.KeyChunkSize"/>.
    /// </summary>
    /// <remarks>
    /// This is where the default is declared, and a host changes it at registration, before <c>Register</c> runs; a value below one is
    /// refused there. It is a tuning number: a larger one makes fewer statements for a set of rows, a smaller one carries fewer
    /// parameters in each. It is the number the readers used before it was a setting.
    /// </remarks>
    public static int KeyChunkSize { get; set; } = 1000;

    /// <summary>
    /// How often a host that keeps a result cache looks at the configuration changes the other hosts published, and drops them from
    /// its cache (FDW-4782), when the setting <c>CacheChangePollIntervalSeconds</c> states no number.
    /// </summary>
    /// <remarks>
    /// This is where the default is declared, and a host changes it at registration, before <c>Register</c> runs; a value that is not
    /// positive is refused there. A number the setting states wins. It is a tuning number: a shorter one shows another host's write
    /// sooner, a longer one reads the change tokens less often.
    /// </remarks>
    public static TimeSpan CacheChangePollInterval { get; set; } = TimeSpan.FromSeconds(10);


    /// <summary>
    /// The connection an endpoint's bare, unnamed <see cref="IConfigurationGateway"/> resolves
    /// through: the one the host declared as <c>ConnectionRoles.Platform</c>.
    /// </summary>
    public static string ConfigurationConnection => PlatformConnection.Name;

    /// <summary>
    /// Whether configuration changes are published to the other hosts and looked at from them (FDW-4782): only when the platform
    /// store is not the server tier. A host whose platform store is the server tier's file store (such as an MCP host that keeps all
    /// its configuration there) has no store the other hosts' caches read, and that tier is not changed at runtime.
    /// </summary>
    public static bool PublishesCacheChanges
        => !string.Equals(ConfigurationConnection, ServerTierConnectionName, StringComparison.Ordinal);

    /// <summary>
    /// Sets this collection's phases: the schema and the gateways over it, both in Register.
    /// </summary>
    /// <remarks>
    /// One phase and not two because neither body did anything but write to the container, and a
    /// service registration is what Register is for. Nor was there an ordering constraint to
    /// express: <c>Build</c> resolves the schema off the service provider when the gateway is
    /// resolved, not when it is registered, so the schema only has to be in the container before
    /// anything asks for a gateway.
    ///
    /// What that buys is the caller. This domain is the one a host brings up by hand - it needs a
    /// gateway before the collect runs, to read its own server configuration - and bringing it up
    /// is now a single Register call rather than a Configure that must not be forgotten.
    /// </remarks>
    static ConfigurationGatewayTypes()
    {
        var collectOptions = RegisterFunc;

        Registration((builder, loggerFactory) =>
        {
            // A chunk of no keys reads nothing: refused here, where a host sets it, and not found later as a read that never ends.
            if (KeyChunkSize < 1)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    ConfigurationGatewayProviderLog.KeyChunkSizeInvalid(
                        loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance,
                        KeyChunkSize));
            }

            // An interval of no time looks without pause: refused here, where a host sets it.
            if (CacheChangePollInterval <= TimeSpan.Zero)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    CacheChangeLog.DeclaredIntervalInvalid(
                        loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance,
                        CacheChangePollInterval));
            }

            var schema = ConfigurationSchemaLoader.Load(SchemaFileName);
            builder.Services.TryAddSingleton(schema);

            // The host's own declaration of which connection plays which role, recorded now, before anything reads a
            // holder. A host that declares none does not start; nothing here names a connection for it.
            var declared = ConnectionRoleDeclaration.Declare(
                schema,
                builder.Environment.ApplicationName,
                loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                    ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance);
            if (declared.IsFailure)
                return declared.ToNewResult<IHostApplicationBuilder>();

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            // The server tier's own cache and its setting, stated here because nothing else can state them.
            // That tier is what every other configuration is read through, this gateway's own cache setting
            // (a row of the data gateway domain) included, so the setting cannot be read from it; and it is
            // built inside this body, before the data gateway domain registers the shared cache.
            // The cache is created privately and handed to that one gateway. It is deliberately NOT
            // registered: Build looks up DataGatewayResultCache for every OTHER tier, and a second
            // registration of that type would replace or shadow the platform tier's cache. It lives as long
            // as the process, like the shared one.
            // Entries expire on the default cache duration (five minutes). ServerConfiguration is not
            // meant to change without a restart, so expiry buys nothing here; keeping entries for the life
            // of the process is a duration chosen where each command is built, not in this body.
            var serverTierCache = new DataGatewayResultCache(new MemoryCache(new MemoryCacheOptions()), loggerFactory);
            var serverTierOptions = new MainDataGatewayConfiguration { IsEnabled = true, EnableCache = true };

            // Tells the other hosts which configuration a write here changed, by a token per tag in the platform store
            // (FDW-4782). Every gateway but the server tier's hands it the tags it changed, whether or not this host keeps
            // a cache, since the other hosts may. Asked for the platform gateway at each flush, not now: the gateways are
            // what take it.
            builder.Services.TryAddSingleton<ICacheChangePublisher>(sp =>
                new CacheChangePublisher(
                    () => sp.GetRequiredService<IConfigurationGatewayProvider>().Get(ConfigurationConnection),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    CacheChangeTokens.Target,
                    sp.GetService<ILogger<CacheChangePublisher>>()));

            builder.Services.TryAddSingleton(sp =>
                new ConfigurationGatewayProvider(
                    connectionName => Build(sp, connectionName, loggerFactory, serverTierCache, serverTierOptions),
                    sp.GetService<ILogger<ConfigurationGatewayProvider>>()));
            builder.Services.TryAddSingleton<IConfigurationGatewayProvider>(sp => sp.GetRequiredService<ConfigurationGatewayProvider>());
            builder.Services.TryAddSingleton<IDatabaseGenerationCatalog>(sp => sp.GetRequiredService<ConfigurationGatewayProvider>());

            // Looks at each configuration database now and then and empties the shared result cache when one was reset or
            // cannot be reached. Takes the cache the gateways take, and how often to look from the setting ResetWatchIntervalSeconds
            // (FDW-3207); a host with no cache or no settings says so and watches nothing.
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DatabaseResetWatcher>(DatabaseResetWatcher.Create));

            // Drops from this host's cache the configuration other hosts changed, from the tokens they publish (FDW-4782).
            // A host with no cache looks at nothing; a host with one looks as often as the setting
            // CacheChangePollIntervalSeconds states, or CacheChangePollInterval declares when it states nothing.
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CacheChangePoller>(CacheChangePoller.Create));

            // The declared connections themselves, for platform plumbing that needs one by name -- the credential
            // vault over AuthDb. Same source and factory resolution as the gateways above, so a connection the host
            // declares is reachable without a ConfigurationDb row, and one it does not declare is refused (FDW-1084).
            builder.Services.TryAddSingleton<IDeclaredConnectionProvider>(sp =>
                new DeclaredConnectionProvider(
                    sp.GetRequiredService<ConfigurationSchema>(),
                    sp,
                    sp.GetService<ILogger<DeclaredConnectionProvider>>()));

            // Endpoint bases take IConfigurationGateway directly rather than the provider -- this is
            // the bare, unnamed instance for ConfigurationConnection. Resolved now, from a snapshot,
            // rather than lazily inside a DI factory: a factory delegate has no GenericResult channel,
            // only throw, and this codebase fails loud through GenericResult instead. Get() memoizes,
            // so this doesn't duplicate the gateway a named lookup later produces for the same
            // connection -- it just builds it here instead of at first ask.
            using var built = builder.Services.BuildServiceProvider();
            var gateways = built.GetRequiredService<IConfigurationGatewayProvider>();

            // ServerConfiguration first, and deliberately. It is the store the logging domain reads
            // its own rows from, and it is reached over a FileSystem connection -- no secret, no
            // database. Building it first means logging can stand up before anything opens
            // PlatformConfiguration, which is MsSql and whose factory asks for a logger. Built the
            // other way round, that request re-enters Serilog's configure callback, which reads the
            // logging row, which comes back through this gateway: a cycle DI turns into
            // cross-thread waits, so the host parks with no exception and no log line.
            // Get() memoizes, so the named lookup a caller makes later reuses this instance.
            var serverGateway = gateways.Get(ServerTierConnectionName);
            if (serverGateway.IsFailure || serverGateway.Value is null)
            {
                var serverLog = loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                    ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance;
                return GenericResult<IHostApplicationBuilder>.Failure(
                    ConfigurationGatewayProviderLog.BareGatewayUnavailable(
                        serverLog, ServerTierConnectionName, serverGateway.CurrentMessage ?? string.Empty));
            }

            var bareGateway = gateways.Get(ConfigurationConnection);
            if (bareGateway.IsFailure || bareGateway.Value is null)
            {
                var log = loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                    ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance;
                return GenericResult<IHostApplicationBuilder>.Failure(
                    ConfigurationGatewayProviderLog.BareGatewayUnavailable(
                        log, ConfigurationConnection, bareGateway.CurrentMessage ?? string.Empty));
            }

            // The container the cache change tokens live in has to be one the host declares: without it no write here
            // reaches the other hosts, and nothing would say so until their caches served old values (FDW-4782).
            var tokenLog = loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance;
            var tokenContainer = PublishesCacheChanges
                ? TokenContainerDeclared(bareGateway.Value, tokenLog)
                : GenericResult.Success();
            if (tokenContainer.IsFailure)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(
                    CacheChangeLog.TokenContainerNotDeclared(
                        tokenLog,
                        CacheChangeTokens.ContainerName,
                        CacheChangeTokens.PathName,
                        CacheChangeTokens.Target.DataStore,
                        tokenContainer.FailureReason().Message));
            }

            builder.Services.TryAddScoped(_ => bareGateway.Value);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    // Whether the platform store declares the token container: the store, its path, then the container.
    private static IGenericResult TokenContainerDeclared(IConfigurationGateway gateway, ILogger logger)
    {
        var store = gateway.DataStores.FirstOrDefault(
            s => string.Equals(s.Name, CacheChangeTokens.Target.DataStore, StringComparison.OrdinalIgnoreCase));
        if (store is null)
            return GenericResult.Failure(ConfigurationGatewayLog.ResolveContainerStoreNotFound(
                logger, CacheChangeTokens.Target.DataStore, gateway.DataStores.Count, string.Join(", ", gateway.DataStores.Select(s => s.Name))));

        var path = store.Path(CacheChangeTokens.PathName);
        if (!path.IsSuccess || path.Value is null)
            return path;

        var container = path.Value.Container(CacheChangeTokens.ContainerName);
        return container.IsSuccess && container.Value is not null ? GenericResult.Success() : container;
    }

    /// <summary>The connection this framework's own configuration is read through.</summary>
    private static string ServerTierConnectionName => ServerConnection.Name;

    private static IGenericResult<IConfigurationGateway> Build(
        System.IServiceProvider services,
        string connectionName,
        ILoggerFactory? loggerFactory,
        DataGatewayResultCache serverTierCache,
        MainDataGatewayConfiguration serverTierOptions)
    {
        var log = loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance;

        var schema = services.GetRequiredService<ConfigurationSchema>();
        var declared = schema.Connections.FirstOrDefault(
            c => string.Equals(c.Name, connectionName, StringComparison.OrdinalIgnoreCase));

        if (declared is null)
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionNotDeclared(log, connectionName));

        if (string.IsNullOrWhiteSpace(declared.Implementation))
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionDeclaresNoKind(log, connectionName));

        if (ConnectionTypes.ByName(declared.Implementation) is not IServiceType connectionType)
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionKindNotRegistered(
                    log, connectionName, declared.Implementation));

        // Resolve the FACTORY, not the connection's own implementation provider. Everything this
        // gateway touches has to exist before any configuration has been read, and an
        // implementation provider pulled from the container brings its whole graph -- a
        // secret-manager provider, a logger -- which reaches the logging domain, which reads its
        // configuration back through here. That cycle parks the host silently. The connections a
        // gateway opens declare no secret, so there is nothing for a richer provider to do.
        if (services.GetService(connectionType.FactoryType) is not IConnectionFactory factory)
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionFactoryUnavailable(
                    log, connectionName, connectionType.FactoryType.Name));

        var connectionProvider = new ConfigurationConnectionProvider(factory);


        return GenericResult<IConfigurationGateway>.Success(
            new ConfigurationGateway(
                connectionName,
                connectionProvider,
                schema,
                services.GetService<ILogger<ConfigurationGateway>>(),
                // Why conditional: the gateway onto the server tier is what the configuration is read
                // through, so asking for that configuration while building it would recurse. It is given the
                // cache and setting the Registration body states for it; every other tier reads its setting
                // from the configuration, and shares the cache the data gateway domain registers.
                string.Equals(connectionName, ServerTierConnectionName, System.StringComparison.Ordinal)
                    ? serverTierCache
                    : services.GetService<DataGatewayResultCache>(),
                string.Equals(connectionName, ServerTierConnectionName, System.StringComparison.Ordinal)
                    ? serverTierOptions
                    : services.GetService<MainDataGatewayConfiguration>(),
                services.GetService<IAuthenticationContextAccessor>(),
                // The server tier is a file store no other host caches, and is not changed at runtime: it publishes nothing,
                // and nothing is published at all when the platform store is that tier (there is no token container to write).
                string.Equals(connectionName, ServerTierConnectionName, System.StringComparison.Ordinal) || !PublishesCacheChanges
                    ? null
                    : services.GetService<ICacheChangePublisher>()));
    }


}
