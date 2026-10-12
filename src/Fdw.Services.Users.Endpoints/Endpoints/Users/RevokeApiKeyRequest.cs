using System;
using FastEndpoints;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to revoke one of an account's API keys.
/// </summary>
public class RevokeApiKeyRequest : UserScopedRequest
{
    /// <summary>Gets or sets the key to revoke.</summary>
    public Guid KeyId { get; set; }

    /// <summary>
    /// Gets or sets the kind of the key being revoked: <c>user</c> or <c>agent</c>. The two live in
    /// different stores, so the kind says which one to ask rather than leaving it to be guessed.
    /// </summary>
    /// <remarks>
    /// Bound from the query string — <c>?kind=agent</c> — because a DELETE carries no body to read
    /// it from.
    /// </remarks>
    [QueryParam]
    public string Kind { get; set; } = string.Empty;
}
