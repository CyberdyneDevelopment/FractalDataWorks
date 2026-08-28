using System;
using System.Threading.Tasks;

namespace Fdw.Services.Messaging.Hubs;

/// <summary>
/// Strongly-typed client interface for the message hub.
/// </summary>
/// <remarks>
/// The message hub broadcasts through this typed-client contract, the same way every other FDW
/// real-time hub does.
/// </remarks>
public interface IMessageHubClient
{
    /// <summary>Notifies the recipient that a new message has arrived.</summary>
    /// <param name="message">The message that arrived.</param>
    /// <remarks>
    /// The whole message travels rather than its id. A burst of turns arriving together would
    /// otherwise become one fetch per turn, and a transcript that paints in a stagger.
    /// </remarks>
    Task NewMessage(MessagePayload message);

    /// <summary>Notifies the recipient that their unread message count has changed.</summary>
    Task UnreadCountChanged();

    /// <summary>Notifies the recipient that a message was marked read.</summary>
    /// <param name="messageId">The identifier of the message that was read.</param>
    Task MessageRead(Guid messageId);
}
