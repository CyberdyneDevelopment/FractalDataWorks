using System;
using System.Security.Claims;
using System.Threading;
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

        if (!await AuthorizeJoin(scopeKey, Context.ConnectionAborted).ConfigureAwait(false))
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
    /// <para>
    /// A <c>user:</c> scope (<see cref="HubScopes.User"/>) is the group of one person and a connection may join only its own: the key must name the
    /// caller's id, and a connection with no id joins no user scope. An <c>org:</c> scope (<see cref="HubScopes.Org"/>) is the group of one
    /// organization and a connection may join only the organization its <c>org_id</c> claim names. Without this a signed-in person could name
    /// another person's or organization's scope and receive what is published there (FDW-2792).
    /// </para>
    /// <para>
    /// Any other key is a resource scope such as <c>execution:{id}</c>; whether a caller may watch that resource is a rule only the hub knows, so the
    /// default allows it and a hub that has the rule overrides this method.
    /// </para>
    /// </remarks>
    protected virtual bool CanJoin(string scopeKey)
    {
        if (scopeKey.StartsWith(HubScopes.UserPrefix, StringComparison.Ordinal))
        {
            return CallerUserId is { } caller
                && string.Equals(scopeKey, HubScopes.User(caller), StringComparison.OrdinalIgnoreCase);
        }

        if (scopeKey.StartsWith(HubScopes.OrgPrefix, StringComparison.Ordinal))
        {
            return Context.User?.FindFirst("org_id")?.Value is { Length: > 0 } org
                && scopeKey.StartsWith(string.Concat(HubScopes.OrgPrefix, org, ":"), StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    /// <summary>
    /// Authorizes a <see cref="Subscribe"/> request when the answer has to be looked up.
    /// </summary>
    /// <param name="scopeKey">The requested group key.</param>
    /// <param name="cancellationToken">Cancelled when the connection is aborted.</param>
    /// <returns><see langword="true"/> if the connection may join the group.</returns>
    /// <remarks>
    /// <see cref="Subscribe"/> asks this method. The default answers <see cref="CanJoin"/>, so a hub whose rule needs no lookup overrides
    /// <see cref="CanJoin"/> and nothing else. A hub that must ask a store whose a resource is (FDW-2804) overrides this method, calls
    /// <see cref="CanJoin"/> first so the person and organization rule still applies, and refuses when the owner cannot be found.
    /// </remarks>
    protected virtual Task<bool> AuthorizeJoin(string scopeKey, CancellationToken cancellationToken)
        => Task.FromResult(CanJoin(scopeKey));

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

        return JoinScope(HubScopes.User(userId));
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

    /// <summary>
    /// Gets the tenant the connection is signed in under: the <c>tenantId</c> claim, and nothing else.
    /// </summary>
    /// <remarks><see langword="null"/> when the connection carries no tenant. A hub that compares a resource's tenant to this one refuses on <see langword="null"/>.</remarks>
    protected string? CallerTenantId
    {
        get
        {
            var tenant = Context.User?.FindFirst("tenantId")?.Value;
            return string.IsNullOrEmpty(tenant) ? null : tenant;
        }
    }

    /// <summary>
    /// Gets the id of the person the connection is signed in as: the <c>sub</c> claim, else the name identifier, and nothing else.
    /// </summary>
    /// <remarks>
    /// This is the person's id, which is what scopes and message recipients are named by. It does not fall back to the display name, which two people
    /// can share and which is not an id. <see langword="null"/> when the connection carries neither claim.
    /// </remarks>
    protected string? CallerUserId
    {
        get
        {
            var sub = Context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub))
            {
                return sub;
            }

            var nameIdentifier = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(nameIdentifier) ? null : nameIdentifier;
        }
    }
}
