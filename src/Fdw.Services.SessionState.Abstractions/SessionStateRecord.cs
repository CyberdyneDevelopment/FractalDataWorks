using System;
using Fdw.Data;

namespace Fdw.Services.SessionState;

/// <summary>
/// Data record for persisted session state entries.
/// </summary>
[GenerateMapper]
public sealed class SessionStateRecord
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user identifier (durable GUID from auth).</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the state key in format {domain}:{page}:{component}.</summary>
    public string StateKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized state value (JSON).</summary>
    public string StateValue { get; set; } = string.Empty;

    /// <summary>Gets or sets when the record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the record was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets when the record expires, or null for no expiration.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
