using System;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Represents a saved connection configuration.
/// </summary>
public sealed class SavedConnection
{
    /// <summary>
    /// Gets or sets the connection name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the instance URL.
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets when this connection was last used.
    /// </summary>
    public DateTime? LastUsed { get; set; }
}