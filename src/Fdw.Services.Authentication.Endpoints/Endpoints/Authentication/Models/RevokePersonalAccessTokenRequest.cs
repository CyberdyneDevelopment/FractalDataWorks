using System;

namespace Fdw.Services.Authentication.Endpoints.Models;

/// <summary>
/// Request model for revoking a personal access token.
/// </summary>
public class RevokePersonalAccessTokenRequest
{
    /// <summary>
    /// Gets or sets the token ID to revoke (from route).
    /// </summary>
    public Guid TokenId { get; set; }
}
