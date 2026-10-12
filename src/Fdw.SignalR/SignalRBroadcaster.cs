using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.SignalR;

/// <summary>
/// Base class for SignalR broadcasters that send events to hub clients.
/// </summary>
/// <typeparam name="THub">The SignalR hub type.</typeparam>
/// <typeparam name="TClient">The strongly-typed client interface.</typeparam>
/// <remarks>
/// <para>
/// Provides common infrastructure for broadcasting events to SignalR groups:
/// <list type="bullet">
/// <item><description>Entity-specific groups: <c>entity:{id}</c></description></item>
/// <item><description>User-specific groups: <c>user:{userId}</c></description></item>
/// <item><description>Global groups: <c>all-{entities}</c></description></item>
/// </list>
/// </para>
/// <para>
/// Derived classes implement specific broadcast methods that call
/// <see cref="BroadcastToGroups{TEvent}"/> with the appropriate event and groups.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PipelineStatusBroadcaster : SignalRBroadcaster&lt;PipelineStatusHub, IPipelineStatusHubClient&gt;
/// {
///     public Task BroadcastStatusChange(PipelineStatusUpdate update) =&gt;
///         BroadcastToGroups(
///             update,
///             (client, evt) =&gt; client.OnStatusChanged(evt),
///             "pipeline-updates",
///             $"pipeline:{update.PipelineName}");
/// }
/// </code>
/// </example>
public abstract class SignalRBroadcaster<THub, TClient>
    where THub : Hub<TClient>
    where TClient : class
{
    /// <summary>
    /// Gets the SignalR hub context for sending messages.
    /// </summary>
    protected IHubContext<THub, TClient> HubContext { get; }

    /// <summary>
    /// Gets the logger for this broadcaster.
    /// </summary>
    protected ILogger<SignalRBroadcaster<THub, TClient>> Logger { get; }

    /// <summary>
    /// Gets the broadcaster name for logging purposes.
    /// </summary>
    protected virtual string BroadcasterName => GetType().Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRBroadcaster{THub, TClient}"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="logger">The logger instance.</param>
    protected SignalRBroadcaster(
        IHubContext<THub, TClient> hubContext,
        ILogger<SignalRBroadcaster<THub, TClient>>? logger = null)
    {
        HubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        Logger = logger ?? NullLogger<SignalRBroadcaster<THub, TClient>>.Instance;
    }

    /// <summary>
    /// Broadcasts an event to multiple SignalR groups.
    /// </summary>
    /// <typeparam name="TEvent">The event type to broadcast.</typeparam>
    /// <param name="evt">The event to broadcast.</param>
    /// <param name="send">Function that invokes the client method with the event.</param>
    /// <param name="groups">The group names to broadcast to.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    protected async Task BroadcastToGroups<TEvent>(
        TEvent evt,
        Func<TClient, TEvent, Task> send,
        params string[] groups)
    {
        if (groups.Length == 0)
        {
            return;
        }

        try
        {
            var eventName = typeof(TEvent).Name;
            SignalRLog.BroadcastStarting(Logger, BroadcasterName, eventName, groups.Length);

            var tasks = groups.Select(group =>
                send(HubContext.Clients.Group(group), evt));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            SignalRLog.BroadcastCompleted(Logger, BroadcasterName, eventName, groups.Length);
        }
        catch (Exception ex)
        {
            var eventName = typeof(TEvent).Name;
            SignalRLog.BroadcastFailed(Logger, ex, BroadcasterName, eventName);
        }
    }

    /// <summary>
    /// Broadcasts an event to a single SignalR group.
    /// </summary>
    /// <typeparam name="TEvent">The event type to broadcast.</typeparam>
    /// <param name="evt">The event to broadcast.</param>
    /// <param name="send">Function that invokes the client method with the event.</param>
    /// <param name="group">The group name to broadcast to.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    protected async Task BroadcastToGroup<TEvent>(
        TEvent evt,
        Func<TClient, TEvent, Task> send,
        string group)
    {
        try
        {
            var eventName = typeof(TEvent).Name;
            SignalRLog.BroadcastStarting(Logger, BroadcasterName, eventName, 1);

            await send(HubContext.Clients.Group(group), evt).ConfigureAwait(false);

            SignalRLog.BroadcastCompleted(Logger, BroadcasterName, eventName, 1);
        }
        catch (Exception ex)
        {
            var eventName = typeof(TEvent).Name;
            SignalRLog.BroadcastFailed(Logger, ex, BroadcasterName, eventName);
        }
    }

    /// <summary>
    /// Broadcasts an event to all connected clients.
    /// </summary>
    /// <typeparam name="TEvent">The event type to broadcast.</typeparam>
    /// <param name="evt">The event to broadcast.</param>
    /// <param name="send">Function that invokes the client method with the event.</param>
    /// <returns>A task representing the broadcast operation.</returns>
    protected async Task BroadcastToAll<TEvent>(
        TEvent evt,
        Func<TClient, TEvent, Task> send)
    {
        try
        {
            var eventName = typeof(TEvent).Name;
            SignalRLog.BroadcastToAllStarting(Logger, BroadcasterName, eventName);

            await send(HubContext.Clients.All, evt).ConfigureAwait(false);

            SignalRLog.BroadcastToAllCompleted(Logger, BroadcasterName, eventName);
        }
        catch (Exception ex)
        {
            var eventName = typeof(TEvent).Name;
            SignalRLog.BroadcastFailed(Logger, ex, BroadcasterName, eventName);
        }
    }
}
