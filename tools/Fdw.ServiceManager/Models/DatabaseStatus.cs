using System;

namespace Fdw.ServiceManager.Models;

/// <summary>
/// Status of the Docker/SQL Server database.
/// </summary>
public sealed class DatabaseStatus
{
    /// <summary>
    /// Gets or sets whether Docker is available.
    /// </summary>
    public bool DockerAvailable { get; set; }

    /// <summary>
    /// Gets or sets whether the SQL Server container is running.
    /// </summary>
    public bool SqlServerRunning { get; set; }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether the database is ready for use.
    /// </summary>
    public bool IsReady => DockerAvailable && SqlServerRunning;

    /// <summary>
    /// Gets the Spectre.Console markup for status.
    /// </summary>
    public string StatusMarkup => IsReady
        ? "[green]● Connected[/]"
        : "[red]○ Not Available[/]";

    /// <summary>
    /// Creates status indicating Docker is not available.
    /// </summary>
    public static DatabaseStatus DockerNotAvailable(string message) => new()
    {
        DockerAvailable = false,
        SqlServerRunning = false,
        Message = message
    };

    /// <summary>
    /// Creates status indicating SQL Server is not running.
    /// </summary>
    public static DatabaseStatus SqlServerNotRunning(string message) => new()
    {
        DockerAvailable = true,
        SqlServerRunning = false,
        Message = message
    };

    /// <summary>
    /// Creates status indicating everything is ready.
    /// </summary>
    public static DatabaseStatus Ready() => new()
    {
        DockerAvailable = true,
        SqlServerRunning = true,
        Message = "SQL Server container running"
    };
}
