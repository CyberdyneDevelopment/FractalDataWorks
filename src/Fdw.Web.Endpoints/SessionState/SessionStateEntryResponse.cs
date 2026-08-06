using System;
using System.Text.Json;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>Response model for a session state entry.</summary>
public sealed class SessionStateEntryResponse
{
    /// <summary>Gets or sets the state key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON value.</summary>
    public JsonElement Value { get; set; }

    /// <summary>Gets or sets when the entry was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets when the entry expires.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
