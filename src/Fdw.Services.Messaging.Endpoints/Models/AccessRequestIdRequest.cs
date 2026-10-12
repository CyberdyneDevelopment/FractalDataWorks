using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request for getting an access request by ID.
/// </summary>
public class AccessRequestIdRequest
{
    /// <summary>
    /// Gets or sets the access request identifier.
    /// </summary>
    public Guid Id { get; set; }
}
