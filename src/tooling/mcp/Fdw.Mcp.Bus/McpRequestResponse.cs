using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Mcp.Bus.Abstractions;
using Fdw.Mcp.Bus.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>
/// Request/response helper for bus-mediated tool invocations. A caller (e.g. the Pidgin SQL
/// participant) publishes an invocation event and awaits the first matching result or error
/// event by <see cref="McpEvent.CorrelationId"/>.
/// </summary>
public static class McpRequestResponse
{
    /// <summary>
    /// Publishes a tool invocation and awaits the matching result. Returns a failed result, logged,
    /// when the tool reports an error, when <paramref name="timeout"/> elapses with no answer, or when
    /// the subscription closes first.
    /// </summary>
    /// <param name="bus">The event bus to publish on and await a response from.</param>
    /// <param name="serverName">The MCP server the tool belongs to.</param>
    /// <param name="toolName">The tool to invoke.</param>
    /// <param name="args">The tool arguments.</param>
    /// <param name="view">The canvas view intent for the invocation.</param>
    /// <param name="timeout">How long to wait for a response before giving up.</param>
    /// <param name="logger">Logger for the failure paths; may be null.</param>
    /// <param name="causation">The event that caused this invocation, when there is one.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <remarks>
    /// Why the timeout is a required parameter rather than an optional one with a default: the
    /// response comes from another process, which may never answer — a crashed server, a dropped
    /// event, a mis-routed correlation id. Without a bound the caller does not fail, it stops, and a
    /// caller that stops reports nothing at all. There is no timeout this layer could pick on the
    /// caller's behalf that would be right for every tool, so the caller states it.
    /// </remarks>
    public static async Task<IGenericResult<McpEvent>> InvokeAndAwait<TArgs>(
        IMcpEventBus bus,
        string serverName,
        string toolName,
        TArgs args,
        IViewIntent view,
        TimeSpan timeout,
        ILogger? logger = null,
        ulong? causation = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bus);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "A positive timeout is required.");

        logger ??= NullLogger.Instance;
        var correlationId = Guid.NewGuid();
        var resultPattern = McpTopics.ToolResult(serverName, toolName);
        var errorPattern = McpTopics.ToolError(serverName, toolName);

        // Subscribe before publishing so we don't race the response. The linked source also carries
        // the timeout, so an unanswered invocation ends the enumeration rather than waiting forever.
        using var subscriptionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        subscriptionCts.CancelAfter(timeout);
        var subscription = bus.Subscribe($"mcp/{serverName}/{toolName}/*", subscriptionCts.Token);

        await bus.PublishToolInvocation(serverName, toolName, args, view, correlationId, causation, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await foreach (var evt in subscription.ConfigureAwait(false))
            {
                if (evt.CorrelationId != correlationId) continue;
                if (string.Equals(evt.Topic, resultPattern, StringComparison.Ordinal))
                    return GenericResult<McpEvent>.Success(evt);
                if (string.Equals(evt.Topic, errorPattern, StringComparison.Ordinal))
                {
                    return GenericResult<McpEvent>.Failure(
                        McpBusResultCodes.ByName("ToolReportedError"), logger,
                        ResultDetails.Create("server", serverName, "tool", toolName,
                                "error", System.Text.Encoding.UTF8.GetString(evt.Payload.Span))
                            .With("correlationId", correlationId));
                }
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // The linked source carries both the caller's token and the timeout, so a cancellation the
            // caller did not ask for is the timeout firing. Reported as its own failure, or the caller
            // cannot tell an unanswered invocation from their own cancellation. The exception itself is
            // carried in the details rather than discarded — it is the only record of where the wait
            // was when it was abandoned.
            return GenericResult<McpEvent>.Failure(
                McpBusResultCodes.ByName("InvocationTimedOut"), logger,
                ResultDetails.Create("server", serverName, "tool", toolName, "timeout", timeout)
                    .With("correlationId", correlationId)
                    .With("cancellation", ex.Message));
        }
        finally
        {
            await subscriptionCts.CancelAsync().ConfigureAwait(false);
        }

        return GenericResult<McpEvent>.Failure(
            McpBusResultCodes.ByName("SubscriptionClosedBeforeResponse"), logger,
            ResultDetails.Create("server", serverName, "tool", toolName, "correlationId", correlationId));
    }
}

