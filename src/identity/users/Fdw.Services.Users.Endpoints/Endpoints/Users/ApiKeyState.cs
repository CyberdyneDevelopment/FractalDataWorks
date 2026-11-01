using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// An API key the account holds. The raw key is not here: it exists only in the mint response.
/// </summary>
public class ApiKeyState
{
    /// <summary>Gets or sets the key's id, used to revoke it.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Gets or sets the kind: <c>user</c> or <c>agent</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the label shown in listings.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the key's non-secret prefix.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets when the key was minted.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the key stops working, when it expires at all.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Gets or sets when the key was last used to authenticate.</summary>
    public DateTime? LastUsedAt { get; set; }
}
