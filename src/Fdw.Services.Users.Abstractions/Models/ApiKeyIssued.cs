using System;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// A newly minted API key. <see cref="RawKey"/> exists in this response and nowhere else — the
/// store holds only its hash, so a key that is not copied here cannot be recovered.
/// </summary>
public class ApiKeyIssued
{
    /// <summary>Gets or sets the key's id, used to revoke it.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Gets or sets the kind: <c>user</c> or <c>agent</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the label shown in key listings.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw key. Shown once.</summary>
    public string RawKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the key's non-secret prefix, which listings show to identify it.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the moment the key stops working, when it expires at all.</summary>
    public DateTime? ExpiresAt { get; set; }
}
