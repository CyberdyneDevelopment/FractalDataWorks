using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// ServiceTypeOption that registers <c>Lazy&lt;IConfigurationGateway&gt;</c> and
/// <see cref="IConfigurationContainerLookup"/>.
/// <see cref="IConfigurationGateway"/> itself is registered externally via
/// <see cref="ConfigurationGatewayExtensions.AddConfigurationGateway{TConnectionFactory}(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, string)"/>
/// which loads <c>configurationSchema.json</c> via STJ and registers the resulting
/// <see cref="Configuration.ConfigurationSchema"/> singleton.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConfigurationGatewayTypes), "ConfigGateway")]
public sealed class ConfigurationGatewayServiceType : ConfigurationGatewayTypeBase<IGenericService, IConfigurationGatewayFactory>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationGatewayServiceType"/> class.
    /// </summary>
    public ConfigurationGatewayServiceType()
        : base(
            "ConfigGateway",
            "DataGateway:ConfigGateway",
            "Configuration DataGateway",
            "DataGateway variant targeting configuration data (cfg) loaded from configurationSchema.json via STJ deserialization")
    {
        Configuration(builder =>
        {

            // Why: ConfigurationSchema is now loaded from configurationSchema.json via STJ
            // (bypassing IConfiguration binding so that TypeCollection-driven polymorphic dispatch
            // works for ConnectionConfiguration and SecretManagerConfiguration subtypes).
            // AddConfigurationGateway<TFactory>(builder.Services, filePath) registers the singleton.
            // Nothing to bind from IConfiguration here.
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {

            // Why: IConfigurationGateway is NOT registered here. The host registers it via
            // AddConfigurationGateway<TConnectionFactory>() or AddConfigurationGateway<TConnectionFactory, TSecretManager>()
            // so it can supply the concrete factory and optional secret manager without the ServiceTypeCollection
            // 3-phase needing to know the concrete types. This is the explicit exception to the
            // no-extension-methods rule, approved by the user.

            // Why: ImplementationConfigurationProviderBase<TConfig, TCommand> consumes Lazy<IConfigurationGateway>
            // so the gateway resolves on first cfg query, not at domain-registration time.
            builder.Services.TryAddSingleton(sp => new Lazy<IConfigurationGateway>(
                () => sp.GetRequiredService<IConfigurationGateway>()));

            // Why: IConfigurationContainerLookup consumes the gateway's DataStores property
            // (the schema-built tree). The gateway is the single source of truth for the ctrl-tier tree.
            builder.Services.TryAddSingleton<IConfigurationContainerLookup>(sp =>
            {
                var gateway = sp.GetRequiredService<IConfigurationGateway>();
                // Why: Lazy<IReadOnlyList<IDataStore>> wraps the gateway's DataStores property so
                // the Lazy is still available to consumers that expect it, while the actual tree
                // comes from the schema-built path rather than a separate registration.
                var dataStoresLazy = new Lazy<IReadOnlyList<IDataStore>>(
                    () => gateway.DataStores,
                    LazyThreadSafetyMode.ExecutionAndPublication);
                return new ConfigurationContainerLookup(
                    dataStoresLazy,
                    sp.GetService<ILogger<ConfigurationContainerLookup>>());
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
