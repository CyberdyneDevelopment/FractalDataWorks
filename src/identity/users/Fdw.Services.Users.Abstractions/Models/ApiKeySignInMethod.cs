using System;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// An API key to mint with a new user. The raw key is returned once, in the create response.
/// </summary>
public class ApiKeySignInMethod
{
    /// <summary>
    /// Gets or sets the label shown in key listings.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of key: <see cref="ApiKeyKinds.User"/> or <see cref="ApiKeyKinds.Agent"/>.
    /// Required -- the two kinds authenticate as different principals, so neither is assumed.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the moment the key stops working. Null is a key that does not expire.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
