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
/// Separate from <see cref="DataGatewayTypes"/> because the two answer different questions. A data
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
    /// Sets this collection's phases: the schema in Configure, the gateways over it in Register.
    /// </summary>
    static ConfigurationGatewayTypes()
    {
        var collectOptions = RegisterFunc;

        Configuration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton(ConfigurationSchemaLoader.Load(SchemaFileName));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            // Why the provider and not the gateway itself: a domain names the connection its rows live
            // on and asks for the gateway onto it, and that name is settable by a host through
            // PlatformServices.<Domain>.ConfigurationConnectionName. A DI key would fix the choice when
            // the container is built, which is before a host has had the chance to change it.
            builder.Services.TryAddSingleton<IConfigurationGatewayProvider>(sp =>
            {
                var log = sp.GetService<ILogger<ConfigurationGatewayProvider>>();
                var provider = new ConfigurationGatewayProvider(log);
                var schema = sp.GetRequiredService<ConfigurationSchema>();

                // Why every declared connection gets one: configurationSchema.json declares exactly the
                // stores an app must reach before it can read a row, and a configuration gateway differs
                // from its siblings only by which of them it opened.
                foreach (var connection in schema.Connections)
                {
                    var gateway = Build(sp, connection.Name, connection.ServiceOptionType, schema, loggerFactory);
                    if (gateway.IsFailure)
                        return provider;

                    provider.Register(gateway.Value!);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    // Why the factory is resolved from the connection's own declared kind rather than handed in by the
    // host: the schema already says which kind each connection is, and the option for that kind names
    // the factory type it registered. This is what requires connections to register before
    // configuration gateways — the factory must already be in the container when this runs.
    private static IGenericResult<IConfigurationGateway> Build(
        System.IServiceProvider services,
        string connectionName,
        string? serviceOptionType,
        ConfigurationSchema schema,
        ILoggerFactory? loggerFactory)
    {
        var log = loggerFactory?.CreateLogger<ConfigurationGatewayTypes>()
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationGatewayTypes>.Instance;

        if (string.IsNullOrWhiteSpace(serviceOptionType))
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionDeclaresNoKind(log, connectionName));

        if (ConnectionTypes.ByName(serviceOptionType) is not IServiceType connectionType)
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionKindNotRegistered(log, connectionName, serviceOptionType));

        return services.GetService(connectionType.FactoryType) is not IConnectionFactory factory
            ? GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionFactoryUnavailable(
                    log, connectionName, connectionType.FactoryType.Name))
            : GenericResult<IConfigurationGateway>.Success(
                new ConfigurationGateway(
                    connectionName,
                    factory,
                    services.GetService<ISecretManager>(),
                    schema,
                    services.GetService<ILogger<ConfigurationGateway>>(),
                    services.GetService<DataGatewayResultCache>(),
                    services.GetService<IOptions<DataGatewayOptions>>(),
                    services.GetService<IAuthenticationContextAccessor>()));
    }
}
