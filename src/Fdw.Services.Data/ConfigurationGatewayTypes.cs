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

        // Why the schema and not the runtime provider: this connection IS the configuration store, so
        // its own secret manager is the one thing that cannot be read from the store. Passing null sends
        // the factory to the by-name provider, whose Get reads a row through this very gateway - a cycle
        // the Lazy guard reports as "ValueFactory attempted to access the Value property", naming the
        // guard rather than the loop. The schema is the only source available before the store opens.
        ISecretManager? secretManager = null;
        if (schema.SecretManagers.Count > 0)
        {
            var resolved = ResolveBootstrapSecretManager(services, schema, connectionName, log);
            if (resolved.IsFailure)
                return resolved.ToNewResult<IConfigurationGateway>();

            secretManager = resolved.Value;
        }

        return GenericResult<IConfigurationGateway>.Success(
            new ConfigurationGateway(
                connectionName,
                factory,
                secretManager,
                schema,
                services.GetService<ILogger<ConfigurationGateway>>(),
                services.GetService<DataGatewayResultCache>(),
                services.GetService<IOptions<DataGatewayOptions>>(),
                services.GetService<IAuthenticationContextAccessor>()));
    }

    /// <summary>
    /// Builds the secret manager <c>configurationSchema.json</c> declares, through the same
    /// option-to-factory route <see cref="Build"/> uses for the connection.
    /// </summary>
    /// <remarks>
    /// The schema declares only what is needed to REACH the configuration store, so it holds one
    /// secret manager in the ordinary case. Runtime secret managers are rows in that store and are
    /// resolved by the provider, not from here.
    /// </remarks>
    private static IGenericResult<ISecretManager> ResolveBootstrapSecretManager(
        System.IServiceProvider services,
        ConfigurationSchema schema,
        string connectionName,
        ILogger log)
    {
        // Why fail rather than take the first: which manager a connection uses is named on the
        // connection, and that name lives on the concrete connection configuration - unreadable here
        // without knowing the connection kind, which is what this layer exists not to know. Taking one
        // by position would open the store with a credential nobody selected.
        if (schema.SecretManagers.Count > 1)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.BootstrapSecretManagerAmbiguous(
                    log, connectionName, schema.SecretManagers.Count));

        var declared = schema.SecretManagers[0];

        if (string.IsNullOrWhiteSpace(declared.ServiceOptionType))
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.BootstrapSecretManagerDeclaresNoKind(log, declared.Name));

        if (SecretManagerTypes.ByName(declared.ServiceOptionType) is not IServiceType secretManagerType)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.BootstrapSecretManagerKindNotRegistered(
                    log, declared.Name, declared.ServiceOptionType));

        if (services.GetService(secretManagerType.FactoryType) is not IServiceFactory<ISecretManager> factory)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.BootstrapSecretManagerFactoryUnavailable(
                    log, declared.Name, secretManagerType.FactoryType.Name));

        var created = factory.Create(declared);
        return created.IsSuccess && created.Value is not null
            ? created
            : GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.BootstrapSecretManagerCreateFailed(
                    log, declared.Name, created.CurrentMessage?.ToString() ?? "factory returned no secret manager"));
    }
}
