using System;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side request model for creating an agent key.
/// </summary>
public sealed class CreateAgentKeyRequest
{
    /// <summary>
    /// Gets or sets the label for the agent key.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional UTC expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
