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
    /// <param name="userId">The user identifier whose group to join.</param>
    /// <returns>A task representing the subscription operation.</returns>
    public Task JoinUserGroup(string userId) => Subscribe(userId);

    /// <summary>
    /// Removes the connection from its per-user message group.
    /// </summary>
    /// <param name="userId">The user identifier whose group to leave.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public Task LeaveUserGroup(string userId) => Unsubscribe(userId);
}
