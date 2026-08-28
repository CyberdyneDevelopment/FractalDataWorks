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

        // Why null rather than a manager chosen here: the factory resolves the secret manager BY NAME
        // from the connection's own declaration, through the provider it was constructed with. Choosing
        // one at this level means choosing without reading that declaration - and the factory already
        // refuses a manager the connection did not name, so a choice made here is either redundant or
        // wrong.
        return GenericResult<IConfigurationGateway>.Success(
            new ConfigurationGateway(
                connectionName,
                factory,
                null,
                schema,
                services.GetService<ILogger<ConfigurationGateway>>(),
                services.GetService<DataGatewayResultCache>(),
                services.GetService<IOptions<DataGatewayOptions>>(),
                services.GetService<IAuthenticationContextAccessor>()));
    }
}
