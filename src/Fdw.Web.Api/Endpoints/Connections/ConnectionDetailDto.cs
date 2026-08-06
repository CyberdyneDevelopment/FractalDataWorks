using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Services.Connections.Clients.Models;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Detailed DTO for a connection, including server settings, authentication, and timestamps.
/// </summary>
public class ConnectionDetailDto : ResourceDetail
{
    /// <summary>The display value used to mask secret key names in responses.</summary>
    public const string MaskedSecretValue = "\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022";

    /// <summary>Gets or sets the service type name (e.g., "MsSql").</summary>
    public required string ServiceType { get; set; }

    /// <summary>Gets or sets the server hostname or IP address.</summary>
    public required string Server { get; set; }

    /// <summary>Gets or sets the server port number.</summary>
    public int Port { get; set; }

    /// <summary>Gets or sets the database name.</summary>
    public required string Database { get; set; }

    /// <summary>Gets or sets the authentication type discriminator (e.g., "WindowsAuth", "SqlAuth", "EntraId").</summary>
    public string? AuthenticationType { get; set; }

    /// <summary>Gets or sets the authentication key-value properties. Secret values are masked with <see cref="MaskedSecretValue"/>.</summary>
    public IDictionary<string, string?> Authentication { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets whether to trust the server certificate.</summary>
    public bool TrustServerCertificate { get; set; }

    /// <summary>Gets or sets whether the connection uses encryption.</summary>
    public bool Encrypt { get; set; }

    /// <summary>Gets or sets whether the connection is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the last time the connection's schema was discovered.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the connection setup summary, populated when auto-discover is available.</summary>
    public ConnectionSetupSummaryPayload? SetupSummary { get; set; }

    /// <summary>Gets or sets the timestamp of the last connection test, or null if never tested.</summary>
    public DateTimeOffset? LastTestedAt { get; set; }

    /// <summary>Gets or sets whether the last connection test succeeded, or null if never tested.</summary>
    public bool? LastTestSuccess { get; set; }

    /// <summary>Gets or sets the message from the last connection test, or null if never tested.</summary>
    public string? LastTestMessage { get; set; }

    /// <summary>Gets or sets the base URL for HTTP connections.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Gets or sets the HTTP protocol type (Rest, Soap11, Soap12, GraphQL, OData).</summary>
    public string? Protocol { get; set; }

    /// <summary>Gets or sets the request timeout in seconds for HTTP connections.</summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>Gets or sets the security type for HTTP connections (None, BasicAuth, ApiKey, WsSecurity, etc.).</summary>
    public string? SecurityType { get; set; }

    /// <summary>Gets or sets whether the HTTP connection uses mutual TLS.</summary>
    public bool? UseMtls { get; set; }

    /// <summary>Gets or sets whether the automated Connections domain health check probes this connection.</summary>
    public bool HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection is probed once at host startup.</summary>
    public bool HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets the interval, in seconds, between periodic health checks, or null if no periodic interval is configured.</summary>
    public int? HealthCheckIntervalSeconds { get; set; }
}
