using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>DI registration for the MCP event bus and its sinks.</summary>
public static class McpEventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMcpEventBus"/> as a singleton <see cref="InMemoryMcpEventBus"/>,
    /// and the optional file-log sink as a hosted service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the bus options; the caller states the ring capacity, there is no default (FDW-3823).</param>
    public static IServiceCollection AddMcpEventBus(this IServiceCollection services, Action<McpEventBusOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        services.TryAddSingleton<IMcpEventBus>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<McpEventBusOptions>>().Value;
            return new InMemoryMcpEventBus(opts.RingCapacity);
        });

        services.AddHostedService<FileEventLogSink>();

        return services;
    }
}
