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
/// The configuration gateway option. Registers <see cref="IConfigurationContainerLookup"/> over the
/// tree its gateways build.
/// </summary>
/// <remarks>
/// The gateways themselves are built by <see cref="ConfigurationGatewayTypes"/>, one per connection
/// declared in <c>configurationSchema.json</c>, and held by <c>IConfigurationGatewayProvider</c>.
/// </remarks>
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

        Registration((builder, loggerFactory) =>
        {

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
