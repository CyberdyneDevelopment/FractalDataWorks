using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request to change the note of a UI comment.
/// </summary>
public class EditUiFeedbackEndpointRequest
{
    /// <summary>Gets or sets the comment's identifier, from the route.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the new note.</summary>
    public string? Body { get; set; }
}
