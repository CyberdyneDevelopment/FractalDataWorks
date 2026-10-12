namespace Fdw.Mcp.Bus;

/// <summary>Configuration knobs for the in-process MCP event bus.</summary>
public sealed class McpEventBusOptions
{
    /// <summary>Max number of events held in the live ring (and available for in-window replay).</summary>
    public int RingCapacity { get; set; } = 10_000;

    /// <summary>
    /// Optional path to a directory where the file-event-log sink will append events as JSON Lines.
    /// When null, file logging is disabled. When set, the sink is registered as an
    /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/> that subscribes to all events.
    /// </summary>
    public string? FileLogDirectory { get; set; }
}
