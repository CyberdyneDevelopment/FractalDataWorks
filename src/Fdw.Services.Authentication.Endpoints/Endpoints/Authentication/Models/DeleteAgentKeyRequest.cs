using System;

namespace Fdw.Services.Authentication.Endpoints.Models;

/// <summary>
/// Request model for deleting an agent key.
/// </summary>
public class DeleteAgentKeyRequest
{
    /// <summary>
    /// Gets or sets the key ID to delete (from route).
    /// </summary>
    public Guid KeyId { get; set; }
}
