using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request to comment on a UI note.
/// </summary>
public class AddUiFeedbackCommentEndpointRequest
{
    /// <summary>Gets or sets the note's identifier, from the route.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the text of the comment, required, at most 4000 characters.</summary>
    public string? Body { get; set; }
}
