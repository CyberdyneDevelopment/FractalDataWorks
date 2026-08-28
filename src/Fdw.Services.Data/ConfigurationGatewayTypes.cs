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

        ISecretManager? secretManager = null;
        if (schema.SecretManagers.Count > 0)
        {
            var resolved = ResolveSecretManager(services, schema, connectionName, log);
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
    /// Builds the secret manager the schema declares, through the same option-to-factory route
    /// <see cref="Build"/> uses for the connection. Called only when the schema declares at least one.
    /// </summary>
    private static IGenericResult<ISecretManager> ResolveSecretManager(
        System.IServiceProvider services,
        ConfigurationSchema schema,
        string connectionName,
        ILogger log)
    {
        // Why fail rather than pick: a connection does not name its secret manager, so with more than
        // one declared there is no non-arbitrary choice. Taking the first would resolve secrets from a
        // manager nobody selected, and a wrong secret reads as an authentication failure somewhere else.
        if (schema.SecretManagers.Count > 1)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerAmbiguous(
                    log, connectionName, schema.SecretManagers.Count));

        var declared = schema.SecretManagers[0];

        if (string.IsNullOrWhiteSpace(declared.ServiceOptionType))
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerDeclaresNoKind(log, declared.Name));

        if (SecretManagerTypes.ByName(declared.ServiceOptionType) is not IServiceType secretManagerType)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerKindNotRegistered(
                    log, declared.Name, declared.ServiceOptionType));

        if (services.GetService(secretManagerType.FactoryType) is not IServiceFactory<ISecretManager> factory)
            return GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerFactoryUnavailable(
                    log, declared.Name, secretManagerType.FactoryType.Name));

        // Why the header and not declared.Configuration: the factory reads the typed body AND the name
        // off the header, and the name is what the secret manager reports about itself.
        var created = factory.Create(declared);
        return created.IsSuccess && created.Value is not null
            ? created
            : GenericResult<ISecretManager>.Failure(
                ConfigurationGatewayProviderLog.SecretManagerCreateFailed(
                    log, declared.Name, created.CurrentMessage?.ToString() ?? "factory returned no secret manager"));
    }
}
