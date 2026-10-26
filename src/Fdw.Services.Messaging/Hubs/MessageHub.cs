using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.SignalR;

namespace Fdw.Services.Messaging.Hubs;

/// <summary>
/// SignalR hub for real-time message delivery. Clients join a per-user group for targeted delivery.
/// </summary>
/// <remarks>
/// Built on <see cref="RealTimeHubBase{TClient}"/>: lifecycle logging and the uniform subscribe/unsubscribe
/// contract are inherited. The per-user group key is the recipient's user identifier, matching the
/// group <see cref="MessageService"/> publishes to.
/// </remarks>
public sealed class MessageHub : RealTimeHubBase<IMessageHubClient>
{
    /// <inheritdoc/>
    protected override string HubName => "Message";

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageHub"/> class.
    /// </summary>
    /// <param name="logger">The logger for hub lifecycle and subscription events.</param>
    public MessageHub(ILogger<MessageHub> logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Adds the connection to its per-user message group.
    /// </summary>
    /// <param name="userId">The user identifier whose group to join; it must be the caller's own.</param>
    /// <returns>A task representing the subscription operation.</returns>
    /// <remarks>
    /// The group is the shared user scope (<see cref="HubScopes.User"/>), which the base class lets a connection join only for the person it is
    /// signed in as; another person's id is refused and logged (FDW-2792). A Guid is written in the canonical lower-case form
    /// <see cref="MessageService"/> publishes to.
    /// </remarks>
    public Task JoinUserGroup(string userId) => Subscribe(UserScope(userId));

    /// <summary>
    /// Removes the connection from its per-user message group.
    /// </summary>
    /// <param name="userId">The user identifier whose group to leave.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public Task LeaveUserGroup(string userId) => Unsubscribe(UserScope(userId));

    private static string UserScope(string userId)
        => HubScopes.User(Guid.TryParse(userId, out var id) ? id.ToString("D") : userId);
}
