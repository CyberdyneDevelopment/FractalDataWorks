using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
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

            builder.Services.TryAddSingleton<IConfigurationContainerLookup>(sp =>
            {
                // Every connection declared in configurationSchema.json, not one chosen store:
                // Get(configTypeName) scans all DataStores for a container, so a container declared
                // on ServerConfiguration is invisible if only PlatformConfiguration is loaded.
                // The provider builds each gateway on demand from its declaration and caches it.
                var gatewayProvider = sp.GetRequiredService<IConfigurationGatewayProvider>();
                var schema = sp.GetRequiredService<ConfigurationSchema>();
                var dataStoresLazy = new Lazy<IReadOnlyList<IDataStore>>(
                    () =>
                    {
                        var stores = new List<IDataStore>();
                        foreach (var connection in schema.Connections)
                        {
                            var gateway = gatewayProvider.Get(connection.Name);
                            if (!gateway.IsSuccess || gateway.Value is null)
                                continue;

                            stores.AddRange(gateway.Value.DataStores);
                        }

                        return stores;
                    },
                    LazyThreadSafetyMode.ExecutionAndPublication);
                return new ConfigurationContainerLookup(
                    dataStoresLazy,
                    sp.GetService<ILogger<ConfigurationContainerLookup>>());
            });
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
