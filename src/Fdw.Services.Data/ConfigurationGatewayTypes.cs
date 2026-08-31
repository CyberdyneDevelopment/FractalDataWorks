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

            builder.Services.TryAddSingleton<IConfigurationGatewayProvider>(sp =>
                new ConfigurationGatewayProvider(
                    connectionName => Build(sp, connectionName, loggerFactory),
                    sp.GetService<ILogger<ConfigurationGatewayProvider>>()));


            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

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
                services.GetService<IOptions<DataGatewayOptions>>(),
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
