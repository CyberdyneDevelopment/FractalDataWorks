using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request for approving or denying an access request.
/// </summary>
public class ReviewAccessRequestRequest
{
    /// <summary>
    /// Gets or sets the access request identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the reviewer's notes.
    /// </summary>
    public string? Notes { get; set; }
}
