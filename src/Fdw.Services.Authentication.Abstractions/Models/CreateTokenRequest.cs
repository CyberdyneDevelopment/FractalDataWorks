using System;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side request model for creating a personal access token.
/// </summary>
public sealed class CreateTokenRequest
{
    /// <summary>
    /// Gets or sets the user-assigned label for the token.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional UTC expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
