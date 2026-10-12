using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Detailed DTO for a connection, including server settings, authentication, and timestamps.
/// </summary>
public class ConnectionDetailDto : ResourceDetail
{
    /// <summary>The display value used to mask secret key names in responses.</summary>
    public const string MaskedSecretValue = ConnectionSecretMask.Value;

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

    /// <summary>
    /// Gets or sets the schema discovery option the connection uses. Set by the connection endpoints
    /// after the connection type maps the rest of the detail.
    /// </summary>
    public SchemaDiscoveryTypeInfo? SchemaDiscovery { get; set; }

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

    /// <summary>
    /// Gets or sets the named default-value policy this connection resolves through
    /// <c>DefaultValues.ByName(ServiceType).ByName(...)</c>. Null means "use the framework-shipped
    /// <c>"Default"</c> policy". Applies only to relational connection types.
    /// </summary>
    public string? DefaultValuePolicy { get; set; }

    /// <summary>
    /// Gets or sets this connection type's own settings, shaped like its configuration class, with secrets
    /// shown as <see cref="MaskedSecretValue"/>. The same shape a create or update sends in its
    /// <c>Configuration</c>. The flat fields beside it (Server, Database, BaseUrl, ...) are deprecated.
    /// </summary>
    public JsonNode? Configuration { get; set; }

    /// <summary>
    /// Gets or sets, for each credential field the connection's authentication method uses, whether a credential is
    /// stored. Never a value or the secret it is stored in (FDW-1086).
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the connection's configuration does not say which authentication method it uses,
    /// so which credentials it needs is not yet determined; empty for a method that carries none.
    /// </remarks>
    public IReadOnlyList<ConnectionCredentialStatus>? Credentials { get; set; }
}
