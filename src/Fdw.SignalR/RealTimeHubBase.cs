using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.SignalR;

/// <summary>
/// Base class for every FDW real-time SignalR hub.
/// </summary>
/// <typeparam name="TClient">The strongly-typed client interface the hub broadcasts to.</typeparam>
/// <remarks>
/// <para>
/// <see cref="RealTimeHubBase{TClient}"/> is the server-side half of the FDW realtime building block. It owns
/// the connect/disconnect logging, the <see cref="NullLogger{T}"/> fallback, and a uniform group
/// subscription contract (<see cref="Subscribe"/>/<see cref="Unsubscribe"/>) shared by every FDW
/// real-time hub.
/// </para>
/// <para>
/// Derived hubs pair with a <see cref="RealTimeHubOptionBase"/> (declaring route + broadcaster
/// registration) so the hub is discovered and wired through the <see cref="RealTimeHubs"/>
/// collection — never through a per-application hosting extension method.
/// </para>
/// <para>
/// Group membership: the base does <b>not</b> auto-join any group on connect (no global
/// "firehose" by default). A derived hub that needs an automatic group join overrides
/// <see cref="OnJoin"/>; clients otherwise opt in explicitly via <see cref="Subscribe"/>.
/// </para>
/// <para>
/// Authentication is <b>mandatory</b>: the base carries <see cref="AuthorizeAttribute"/> and the
/// building block's endpoint mapping (<see cref="RealTimeHubOptionBase.MapHubAt{THub}"/>) always
/// applies <c>RequireAuthorization</c>. There is no anonymous FDW real-time hub — a hub that needs
/// a broader-than-authenticated policy declares it on its option; one that needs a narrower
/// per-verb policy annotates the verb with <see cref="AuthorizeAttribute"/>.
/// </para>
/// </remarks>
[Authorize]
public abstract class RealTimeHubBase<TClient> : Hub<TClient>
    where TClient : class
{
    /// <summary>
    /// Gets the logger used for hub lifecycle and subscription events.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the short, stable hub name used in log messages (e.g. <c>"PipelineStatus"</c>).
    /// </summary>
    protected abstract string HubName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RealTimeHubBase{TClient}"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger for lifecycle events. When <see langword="null"/> the hub falls back to
    /// <see cref="NullLogger{T}.Instance"/> so the hub remains functional without DI logging.
    /// </param>
    protected RealTimeHubBase(ILogger<RealTimeHubBase<TClient>>? logger = null)
    {
        // Why: NullLogger fallback is the single sanctioned ?? pattern (CLAUDE.md) so the hub is
        // usable when DI does not supply a logger; no other fallback values are permitted here.
        Logger = logger ?? NullLogger<RealTimeHubBase<TClient>>.Instance;
    }

    /// <inheritdoc/>
    public override async Task OnConnectedAsync()
    {
        SignalRLog.ClientConnected(Logger, Context.ConnectionId, HubName);
        await OnJoin().ConfigureAwait(false);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            SignalRLog.ClientDisconnectedWithError(Logger, Context.ConnectionId, HubName, exception.Message);
        }
        else
        {
            SignalRLog.ClientDisconnected(Logger, Context.ConnectionId, HubName);
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes the calling connection to the named scope group.
    /// </summary>
    /// <param name="scopeKey">
    /// The group key to join (for example <c>"execution:{id}"</c> or <c>"pipeline:{name}"</c>).
    /// </param>
    /// <returns>A task representing the subscription operation.</returns>
    /// <remarks>
    /// This is the single, uniform client-facing subscribe verb shared by every FDW hub. An empty
    /// or whitespace key is rejected (logged, not joined) rather than silently joining a malformed
    /// group. Authorization of <paramref name="scopeKey"/> against the caller's tenant/org is a
    /// declared extension point (<see cref="CanJoin"/>).
    /// </remarks>
    public async Task Subscribe(string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            SignalRLog.SubscriptionRejectedEmptyScope(Logger, Context.ConnectionId, HubName);
            return;
        }

        if (!CanJoin(scopeKey))
        {
            SignalRLog.SubscriptionRejectedNotAuthorized(Logger, Context.ConnectionId, HubName, scopeKey);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, scopeKey).ConfigureAwait(false);
        SignalRLog.ClientJoinedGroup(Logger, Context.ConnectionId, scopeKey);
    }

    /// <summary>
    /// Unsubscribes the calling connection from the named scope group.
    /// </summary>
    /// <param name="scopeKey">The group key to leave.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public async Task Unsubscribe(string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            SignalRLog.SubscriptionRejectedEmptyScope(Logger, Context.ConnectionId, HubName);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, scopeKey).ConfigureAwait(false);
        SignalRLog.ClientLeftGroup(Logger, Context.ConnectionId, scopeKey);
    }

    /// <summary>
    /// Override to join the connection to scoped group(s) automatically on connect.
    /// </summary>
    /// <returns>A task representing the auto-join work.</returns>
    /// <remarks>
    /// The default is a no-op: the base never auto-joins a global group. Derived hubs override this
    /// to opt into automatic membership (for example a per-user group derived from the authenticated
    /// principal). Use <see cref="JoinScope"/> to perform the join with consistent logging.
    /// </remarks>
    protected virtual Task OnJoin() => Task.CompletedTask;

    /// <summary>
    /// Override to authorize a <see cref="Subscribe"/> request against the caller's identity/tenant.
    /// </summary>
    /// <param name="scopeKey">The requested group key.</param>
    /// <returns><see langword="true"/> if the connection may join the group.</returns>
    /// <remarks>
    /// The default permits any non-empty key, preserving the pre-existing per-hub behavior. This is
    /// the declared seam where tenant/org-scoped subscription authorization lands (FDW-545 follow-up).
    /// </remarks>
    protected virtual bool CanJoin(string scopeKey) => true;

    /// <summary>
    /// Joins the calling connection to <paramref name="scopeKey"/> with consistent logging.
    /// </summary>
    /// <param name="scopeKey">The group key to join.</param>
    /// <returns>A task representing the join operation.</returns>
    protected async Task JoinScope(string scopeKey)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, scopeKey).ConfigureAwait(false);
        SignalRLog.ClientJoinedGroup(Logger, Context.ConnectionId, scopeKey);
    }

    /// <summary>
    /// Joins the calling connection to its per-user group <c>user:{userId}</c>, using the
    /// authenticated identity.
    /// </summary>
    /// <returns>A task representing the join operation.</returns>
    /// <remarks>
    /// When the connection has no authenticated identity the join is skipped and
    /// <see cref="SignalRLog.HubIdentityMissing"/> is logged — the hub never substitutes a
    /// placeholder identity (no <c>?? "anonymous"</c>). This is the shared user-scope convention for
    /// hubs that auto-join a per-user group.
    /// </remarks>
    protected Task JoinAuthenticatedUserScope()
    {
        var userId = AuthenticatedUserId;
        if (string.IsNullOrEmpty(userId))
        {
            SignalRLog.HubIdentityMissing(Logger, Context.ConnectionId, HubName);
            return Task.CompletedTask;
        }

        return JoinScope($"user:{userId}");
    }

    /// <summary>
    /// Gets the authenticated user identity for the current connection, or <see langword="null"/>
    /// when the connection carries no authenticated identity.
    /// </summary>
    /// <remarks>
    /// Returns the raw identity without substituting a placeholder. Callers that require an identity
    /// must fail loud (log + skip) rather than invent one — there is no <c>?? "anonymous"</c>.
    /// </remarks>
    protected string? AuthenticatedUserId => Context.User?.Identity?.Name;
}
