using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Inline connection options for configuration database.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class InlineConnectionOptions
{
    /// <summary>
    /// Gets or sets the connection type. Default is "MsSql".
    /// </summary>
    public string ConnectionType { get; set; } = "MsSql";

    /// <summary>
    /// Gets or sets the server hostname or IP.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: FdwHost__Configuration__Connection__Server
    /// </remarks>
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the server port. Default is 1433 for SQL Server.
    /// </summary>
    public int Port { get; set; } = 1433;

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: FdwHost__Configuration__Connection__Database
    /// </remarks>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the authentication options.
    /// </summary>
    public AuthenticationOptions? Authentication { get; set; }

    /// <summary>
    /// Gets or sets whether to trust the server certificate. Default is false for production.
    /// </summary>
    public bool TrustServerCertificate { get; set; }

    /// <summary>
    /// Gets or sets the connection timeout in seconds. Default is 30.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds. Default is 30.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
