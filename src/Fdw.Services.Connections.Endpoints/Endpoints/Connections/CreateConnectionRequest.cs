using System.Text.Json;
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

    /// <summary>Request headers sent with every request an HTTP connection makes.</summary>
    /// <remarks>
    /// A header named here overrides the one the factory would otherwise send, which is how a
    /// connection sets its own User-Agent — some hosts answer 200 for one value and 403 for another.
    /// </remarks>
    public IDictionary<string, string?>? Headers { get; set; }

    /// <summary>Security type: None, BasicAuth, ApiKey, WsSecurity, etc.</summary>
    public string? SecurityType { get; set; }

    /// <summary>Security configuration key-value pairs (API key, credentials, etc.).</summary>
    public IDictionary<string, string?>? Security { get; set; }

    /// <summary>Whether to use mutual TLS (client certificate).</summary>
    public bool UseMtls { get; set; }


    /// <summary>Gets or sets whether the automated Connections domain health check probes this connection.</summary>
    public bool HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection should be probed once at host startup.</summary>
    public bool HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets the interval, in seconds, between periodic health checks. Null means no periodic interval.</summary>
    public int? HealthCheckIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the name of the schema discovery option the connection uses: one of the options its
    /// connection type offers (<c>discoveryTypes</c> on <c>/connection-types/{name}/capabilities</c>),
    /// <c>None</c> for a connection that does not discover. Required; there is no default.
    /// </summary>
    public string SchemaDiscovery { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the named default-value policy this connection resolves through
    /// <c>DefaultValues.ByName(ServiceType).ByName(...)</c>. Null means "use the framework-shipped
    /// <c>"Default"</c> policy" — applies only to relational connection types (MsSql, PostgreSql,
    /// Sqlite); ignored for others.
    /// </summary>
    public string? DefaultValuePolicy { get; set; }

    /// <summary>
    /// Gets or sets this connection type's own settings: a JSON object shaped like the type's configuration
    /// class, which OpenAPI publishes as one schema per connection type, chosen by <c>ServiceType</c>. Absent
    /// members take the configuration's defaults.
    /// </summary>
    /// <remarks>
    /// Preferred over the flat fields on this request (Server, Port, Database, Authentication, BaseUrl, ...),
    /// which are deprecated and still accepted: they are read as aliases for the type's configuration
    /// properties, and a member sent here wins over its flat equivalent. They will be removed.
    /// </remarks>
    public JsonElement? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the values of the connection's credentials, keyed by the credential fields its authentication
    /// method uses (for example <c>SecretKeyName</c> for a password, <c>ApiKeySecretName</c> for an API key).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Write-only. The server stores each value in the platform's credential secret manager under a secret it chooses
    /// and points the connection at it; no response, log line or endpoint ever returns a value, and a caller never
    /// names the secret manager or the secret (FDW-1086). Required, in full, when the authentication method uses credentials.
    /// </para>
    /// <para>
    /// The one exception is a platform administrator's (FDW-1327): to keep the credentials in a secret manager that
    /// something else manages, the administrator names the secret manager and each secret in the connection's
    /// authentication values and sends no values here. The platform checks that they can be read and never writes or
    /// deletes them; nobody but a platform administrator may then change them.
    /// </para>
    /// </remarks>
    public IDictionary<string, string>? Credentials { get; set; }
}
