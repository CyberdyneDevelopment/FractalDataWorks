using System;

namespace Fdw.Services.Authentication.Endpoints.Models;

/// <summary>
/// Request model for creating a personal access token.
/// </summary>
public class CreatePersonalAccessTokenRequest
{
    /// <summary>
    /// Gets or sets the user-assigned label for the token.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional UTC expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
