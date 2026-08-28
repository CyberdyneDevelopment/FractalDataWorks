using System;
using FastEndpoints;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request to send a conversational message into a thread.
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// Gets or sets the thread this message belongs to.
    /// </summary>
    /// <remarks>
    /// Required, and never minted by the endpoint. The caller decides whether it is opening a
    /// conversation or continuing one, and a server that invented a thread id when the field was
    /// absent would silently split a continuation into a new thread the other side never reads.
    /// </remarks>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the user this message is addressed to.
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Gets or sets which side of the conversation sent this message.
    /// </summary>
    /// <remarks>
    /// OPTIONAL, and never trusted. The endpoint derives the side from how the caller authenticated
    /// and uses that; a value supplied here is only checked against the derived one and refused if
    /// it contradicts. Leave it unset unless you want that assertion checked.
    /// </remarks>
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the message subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the message body.
    /// </summary>
    public string? Body { get; set; }
}
