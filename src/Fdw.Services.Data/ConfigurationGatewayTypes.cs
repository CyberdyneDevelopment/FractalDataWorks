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
using Fdw.Services.Data.Configuration;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
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
    /// The connection an endpoint's bare, unnamed <see cref="IConfigurationGateway"/> resolves
    /// through. Settable by a host the same way every other domain's <c>ConfigurationConnection</c>
    /// is.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

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
            builder.Services.TryAddSingleton(ConfigurationSchemaLoader.Load(SchemaFileName));

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            builder.Services.TryAddSingleton<IConfigurationGatewayProvider>(sp =>
                new ConfigurationGatewayProvider(
                    connectionName => Build(sp, connectionName, loggerFactory),
                    sp.GetService<ILogger<ConfigurationGatewayProvider>>()));

            // Endpoint bases take IConfigurationGateway directly rather than the provider -- this is
            // the bare, unnamed instance for ConfigurationConnection. Resolved now, from a snapshot,
            // rather than lazily inside a DI factory: a factory delegate has no GenericResult channel,
            // only throw, and this codebase fails loud through GenericResult instead. Get() memoizes,
            // so this doesn't duplicate the gateway a named lookup later produces for the same
            // connection -- it just builds it here instead of at first ask.
            using var built = builder.Services.BuildServiceProvider();
            var bareGateway = built.GetRequiredService<IConfigurationGatewayProvider>().Get(ConfigurationConnection);
            if (bareGateway.IsFailure || bareGateway.Value is null)
            {
                var log = loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
                    ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance;
                return GenericResult<IHostApplicationBuilder>.Failure(
                    ConfigurationGatewayProviderLog.BareGatewayUnavailable(
                        log, ConfigurationConnection, bareGateway.CurrentMessage ?? string.Empty));
            }

            builder.Services.TryAddScoped(_ => bareGateway.Value);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    /// <summary>The connection this framework's own configuration is read through.</summary>
    private const string ServerTierConnectionName = "ServerConfiguration";

    private static IGenericResult<IConfigurationGateway> Build(
        System.IServiceProvider services,
        string connectionName,
        ILoggerFactory? loggerFactory)
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
                services.GetService<DataGatewayResultCache>(),
                // Why conditional: the gateway onto the server tier is what the configuration is
                // read through, so asking for that configuration while building it would recurse.
                string.Equals(connectionName, ServerTierConnectionName, System.StringComparison.Ordinal)
                    ? null
                    : services.GetService<MainDataGatewayConfiguration>(),
                services.GetService<IAuthenticationContextAccessor>()));
    }


}
