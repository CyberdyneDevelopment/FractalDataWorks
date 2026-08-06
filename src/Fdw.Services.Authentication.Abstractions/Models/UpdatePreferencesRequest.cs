using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side request model for updating user preferences.
/// </summary>
public sealed class UpdatePreferencesRequest
{
    /// <summary>
    /// Gets or sets the preferences to set (key/value pairs).
    /// </summary>
    public IDictionary<string, string> Preferences { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
