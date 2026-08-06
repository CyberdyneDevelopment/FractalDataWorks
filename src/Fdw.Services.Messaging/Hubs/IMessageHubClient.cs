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
    /// <param name="messageId">The identifier of the new message.</param>
    Task NewMessage(Guid messageId);

    /// <summary>Notifies the recipient that their unread message count has changed.</summary>
    Task UnreadCountChanged();

    /// <summary>Notifies the recipient that a message was marked read.</summary>
    /// <param name="messageId">The identifier of the message that was read.</param>
    Task MessageRead(Guid messageId);
}
