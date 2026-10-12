using System.Collections.Generic;

namespace Fdw.Web.Endpoints.SessionState;

/// <summary>Response model for listing session state keys.</summary>
public sealed class SessionStateKeysResponse
{
    /// <summary>Gets or sets the list of keys.</summary>
    public IReadOnlyList<string> Keys { get; set; } = [];
}
