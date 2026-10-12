using System;
using System.Text.Json;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>Request model for upserting a session state value.</summary>
public sealed class UpsertSessionStateRequest
{
    /// <summary>Gets or sets the state key (from route).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON value to persist.</summary>
    public JsonElement Value { get; set; }

    /// <summary>Gets or sets an optional expiration time.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
