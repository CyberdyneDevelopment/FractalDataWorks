using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Ergonomic helpers for MCP tool servers to publish invocation / result / error events on the
/// bus. Tool methods call these immediately before returning to the MCP SDK transport so the
/// stdio response and the bus event are produced from the same code path.
/// </summary>
public static class McpToolEventBusExtensions
{
    /// <summary>
    /// Publish a tool-invocation event. Returns the assigned <see cref="McpEvent.EventId"/> so the
    /// caller can use it as the <c>causation</c> for the matching result event.
    /// </summary>
    public static ValueTask<ulong> PublishToolInvocation<TArgs>(
        this IMcpEventBus bus,
        string serverName,
        string toolName,
        TArgs args,
        IViewIntent view,
        Guid correlationId,
        ulong? causation = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(view);

        var payload = JsonSerializer.SerializeToUtf8Bytes(args);
        var draft = new McpEventDraft(
            Topic: McpTopics.ToolInvoke(serverName, toolName),
            CorrelationId: correlationId,
            Causation: causation,
            View: view,
            PayloadType: $"mcp.tool.invoke.{serverName}.{toolName}",
            Payload: payload);

        return bus.Publish(draft, cancellationToken);
    }

    /// <summary>
    /// Publish a tool-result event keyed by the same <paramref name="correlationId"/> as the
    /// invocation, with the invocation's event id as <paramref name="causation"/>.
    /// </summary>
    public static ValueTask<ulong> PublishToolResult<TResult>(
        this IMcpEventBus bus,
        string serverName,
        string toolName,
        TResult result,
        IViewIntent view,
        Guid correlationId,
        ulong? causation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(view);

        var payload = JsonSerializer.SerializeToUtf8Bytes(result);
        var draft = new McpEventDraft(
            Topic: McpTopics.ToolResult(serverName, toolName),
            CorrelationId: correlationId,
            Causation: causation,
            View: view,
            PayloadType: $"mcp.tool.result.{serverName}.{toolName}",
            Payload: payload);

        return bus.Publish(draft, cancellationToken);
    }

    /// <summary>
    /// Publish a tool-error event. The payload is the structured failure message; sinks discriminate
    /// on the <c>error</c> phase rather than parsing the body.
    /// </summary>
    public static ValueTask<ulong> PublishToolError(
        this IMcpEventBus bus,
        string serverName,
        string toolName,
        string errorMessage,
        Guid correlationId,
        ulong? causation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        var draft = new McpEventDraft(
            Topic: McpTopics.ToolError(serverName, toolName),
            CorrelationId: correlationId,
            Causation: causation,
            // Errors are always silent on the canvas; surface them through their own UI affordance.
            View: ViewIntents.ByName("Silent"),
            PayloadType: $"mcp.tool.error.{serverName}.{toolName}",
            Payload: Encoding.UTF8.GetBytes(errorMessage));

        return bus.Publish(draft, cancellationToken);
    }
}
