namespace Fdw.TUI.Management.Services;

/// <summary>
/// Result of a connection attempt.
/// </summary>
public sealed class ConnectionResult
{
    /// <summary>
    /// Gets or sets whether the connection was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if connection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ConnectionResult Succeeded() => new() { Success = true };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ConnectionResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}