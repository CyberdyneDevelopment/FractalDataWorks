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
using Fdw.Hosting.Abstractions.Configuration;
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

            // Why here and not in each host: a provider that decides which connection configuration
            // is read through is this collection's own concern, and every host wrote the same line.
            //
            // Why TryAdd: this is the answer when nothing else has one. A multitenancy option
            // registers its own tenant-aware provider, and because that runs after this and adds
            // rather than tries, it wins -- which is the precedence that was inverted while each
            // host registered the default LAST, after PlatformServices.Register had already put the
            // tenant-aware one in. Configuration then always resolved through the default
            // connection, never the tenant's, and nothing said so.
            builder.Services.TryAddSingleton<IConfigurationConnectionNameProvider, DefaultConfigurationConnectionNameProvider>();

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

        if (string.IsNullOrWhiteSpace(declared.ServiceOptionType))
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionDeclaresNoKind(log, connectionName));

        if (ConnectionTypes.ByName(declared.ServiceOptionType) is not IServiceType connectionType)
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionKindNotRegistered(
                    log, connectionName, declared.ServiceOptionType));

        if (services.GetService(connectionType.FactoryType) is not IConnectionFactory factory)
            return GenericResult<IConfigurationGateway>.Failure(
                ConfigurationGatewayProviderLog.ConnectionFactoryUnavailable(
                    log, connectionName, connectionType.FactoryType.Name));

        // The secret manager that opens the configuration store cannot be read out of that store,
        // so it is the one configurationSchema.json declares. Resolving it by name through
        // ISecretManagerProvider instead would ask the configuration store for the credentials
        // needed to reach the configuration store, which re-enters the connection being built.
        var secretManager = ResolveDeclaredSecretManager(services, schema, connectionName, log);
        if (!secretManager.IsSuccess)
            return secretManager.ToNewResult<IConfigurationGateway>();

        return GenericResult<IConfigurationGateway>.Success(
            new ConfigurationGateway(
                connectionName,
                factory,
                secretManager.Value,
                schema,
                services.GetService<ILogger<ConfigurationGateway>>(),
                services.GetService<DataGatewayResultCache>(),
                // Why conditional: the gateway onto the server tier is what the configuration is
                // read through, so asking for that configuration while building it would recurse.
                string.Equals(connectionName, ServerTierConnectionName, System.StringComparison.Ordinal)
                    ? null
                    : services.GetService<DataGatewayImplementationConfiguration>(),
                services.GetService<IAuthenticationContextAccessor>()));
    }

    /// <summary>
    /// Builds the secret manager the schema declares for opening a configuration connection.
    /// </summary>
    /// <param name="services">The container the secret manager factory is resolved from.</param>
    /// <param name="schema">The schema the gateway is bound from.</param>
    /// <param name="connectionName">The connection being opened, named in failures.</param>
    /// <param name="log">The logger.</param>
    /// <remarks>
    /// A connection that needs no secret — one authenticating as the process identity — declares no
    /// secret manager, and a null return says exactly that. It is not a substitute for one that
    /// failed to build: every failure below returns a failed result instead. The connection's own
    /// authentication type checks the manager it is handed against the name it declares, so the
    /// gateway hands over what the schema declares without reading the connection's body, which
    /// would make the connection's kind visible here.
    /// </remarks>
    private static IGenericResult<ISecretManager?> ResolveDeclaredSecretManager(
        System.IServiceProvider services,
        ConfigurationSchema schema,
        string connectionName,
        ILogger log)
    {
        if (schema.SecretManagers.Count == 0)
            return GenericResult<ISecretManager?>.Success(null);

        if (schema.SecretManagers.Count > 1)
            return GenericResult<ISecretManager?>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerAmbiguous(
                    log, connectionName, string.Join(", ", schema.SecretManagers.Select(s => $"'{s.Name}'"))));

        var declared = schema.SecretManagers[0];

        if (string.IsNullOrWhiteSpace(declared.ServiceOptionType))
            return GenericResult<ISecretManager?>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerDeclaresNoKind(log, declared.Name));

        if (SecretManagerTypes.ByName(declared.ServiceOptionType) is not IServiceType secretManagerType)
            return GenericResult<ISecretManager?>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerKindNotRegistered(
                    log, declared.Name, declared.ServiceOptionType));

        if (services.GetService(secretManagerType.FactoryType) is not IServiceFactory<ISecretManager> secretManagerFactory)
            return GenericResult<ISecretManager?>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerFactoryUnavailable(
                    log, declared.Name, secretManagerType.FactoryType.Name));

        if (declared.Configuration is null)
            return GenericResult<ISecretManager?>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerDeclaresNoBody(log, declared.Name));

        // The composed entry, not its body: the name is on the domain row, and the connection
        // checks the manager it is handed against the name it declares. A factory given the body
        // alone has no name to give the manager it builds and falls back to rendering an id, which
        // then fails that check against the name the schema declared one line above.
        var created = secretManagerFactory.Create(declared);
        return created.IsSuccess
            ? GenericResult<ISecretManager?>.Success(created.Value)
            : created.ToNewResult<ISecretManager?>();
    }

}
