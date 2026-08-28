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
    /// Caller-asserted, and validated against the two conversation types rather than accepted as
    /// free text — the endpoint would otherwise be a way to forge any message type, including the
    /// notification types other parts of the system act on.
    ///
    /// It is asserted rather than derived because nothing in the request identifies the caller as
    /// an agent: an agent acts on behalf of its owner, so its <c>sub</c> claim is that person's.
    /// Deriving this needs an agent claim the PAT middleware does not yet emit.
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
