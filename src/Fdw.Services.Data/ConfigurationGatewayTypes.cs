using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Collection of configuration gateway service types — the gateways that read and write
/// configuration itself.
/// </summary>
/// <remarks>
/// Separate from <see cref="DataGatewayTypes"/> because the two answer different questions. A data
/// gateway reaches a DataStore that configuration describes; a configuration gateway reaches the
/// store that configuration lives in, and is bound from <c>configurationSchema.json</c> before any
/// row is readable. One collection could carry only one of those.
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
    /// Sets this collection's Register body: the option collect, then the gateway provider.
    /// </summary>
    static ConfigurationGatewayTypes()
    {
        var collectOptions = RegisterFunc;

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
                new ConfigurationGatewayProvider(
                    sp.GetService<ILogger<ConfigurationGatewayProvider>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
