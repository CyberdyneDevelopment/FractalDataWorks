using System;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Current connection status.
/// </summary>
public sealed class ConnectionStatus
{
    /// <summary>
    /// Gets or sets whether connected to an instance.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the connected instance name.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Gets or sets the connected instance URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets when the connection was established.
    /// </summary>
    public DateTime? ConnectedAt { get; set; }
}