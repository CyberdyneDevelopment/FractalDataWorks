using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request for getting a message by ID.
/// </summary>
public class MessageIdRequest
{
    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid Id { get; set; }
}
