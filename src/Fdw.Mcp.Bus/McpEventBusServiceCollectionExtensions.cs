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
    public static IServiceCollection AddMcpEventBus(this IServiceCollection services, Action<McpEventBusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null) services.Configure(configure);
        else services.AddOptions<McpEventBusOptions>();

        services.TryAddSingleton<IMcpEventBus>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<McpEventBusOptions>>().Value;
            return new InMemoryMcpEventBus(opts.RingCapacity);
        });

        services.AddHostedService<FileEventLogSink>();

        return services;
    }
}
