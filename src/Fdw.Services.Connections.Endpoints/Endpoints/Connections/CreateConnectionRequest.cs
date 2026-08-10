using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Request DTO for creating a new connection configuration.
/// </summary>
public class CreateConnectionRequest : ResourceCreateRequest
{
    /// <summary>Gets or sets the service type name (e.g., "MsSql").</summary>
    [Required]
    public string ServiceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the server hostname or IP address.</summary>
    [Required]
    public string Server { get; set; } = string.Empty;

    /// <summary>Gets or sets the server port number.</summary>
    public int Port { get; set; } = 1433;

    /// <summary>Gets or sets the database name.</summary>
    [Required]
    public string Database { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication type discriminator (e.g., "WindowsAuth", "SqlAuth", "EntraId").</summary>
    /// <remarks>
    /// Why: empty string, matching <see cref="Database"/>'s pattern, not null — per-service-type
    /// validators (see ConnectionValidators in the consuming application) enforce a real value.
    /// </remarks>
    public string AuthenticationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication key-value properties. Keys and values depend on AuthenticationType.</summary>
    public IDictionary<string, string?> Authentication { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets whether to trust the server certificate.</summary>
    public bool TrustServerCertificate { get; set; }

    /// <summary>Gets or sets whether the connection uses encryption.</summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>Base URL for HTTP connections (e.g., "https://api.open-meteo.com/v1/").</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Protocol type: Rest, Soap11, Soap12, GraphQL, OData.</summary>
    public string? Protocol { get; set; }

    /// <summary>Request timeout in seconds. Defaults to 30.</summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>Security type: None, BasicAuth, ApiKey, WsSecurity, etc.</summary>
    public string? SecurityType { get; set; }

    /// <summary>Security configuration key-value pairs (API key, credentials, etc.).</summary>
    public IDictionary<string, string?>? Security { get; set; }

    /// <summary>Whether to use mutual TLS (client certificate).</summary>
    public bool UseMtls { get; set; }

    // Why: these three carry the opt-in check-settings columns on conn.Connection through to
    // creation. No defaults — an unset HealthCheckEnabled/OnStartup is false (not checked) and an
    // unset HealthCheckIntervalSeconds is null (no periodic interval), matching
    // ConnectionConfiguration's own no-fallback semantics.

    /// <summary>Gets or sets whether the automated Connections domain health check probes this connection.</summary>
    public bool HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection should be probed once at host startup.</summary>
    public bool HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets the interval, in seconds, between periodic health checks. Null means no periodic interval.</summary>
    public int? HealthCheckIntervalSeconds { get; set; }
}
