using System.Text.Json;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Request DTO for updating an existing connection configuration. Nullable properties indicate no change when null.
/// </summary>
public class UpdateConnectionRequest : ResourceUpdateRequest
{
    /// <summary>Gets or sets the updated server hostname or IP address.</summary>
    public string? Server { get; set; }

    /// <summary>Gets or sets the updated server port number.</summary>
    public int? Port { get; set; }

    /// <summary>Gets or sets the updated database name.</summary>
    public string? Database { get; set; }

    /// <summary>Gets or sets the updated authentication type discriminator (e.g., "WindowsAuth", "SqlAuth", "EntraId"). Null means no change.</summary>
    public string? AuthenticationType { get; set; }

    /// <summary>Gets or sets the updated authentication key-value properties. Null means no change; empty dictionary clears all values.</summary>
    public IDictionary<string, string?>? Authentication { get; set; }

    /// <summary>Gets or sets the updated trust server certificate flag.</summary>
    public bool? TrustServerCertificate { get; set; }

    /// <summary>Gets or sets the updated encryption flag.</summary>
    public bool? Encrypt { get; set; }

    /// <summary>Gets or sets the updated active status.</summary>
    public bool? IsActive { get; set; }

    // ── Http / FileSystem / RoslynWorkspace fields ─────────────────────────

    /// <summary>Gets or sets the base URL (Http), root path (FileSystem), or solution path (RoslynWorkspace). Null means no change.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Gets or sets the protocol type (Http: Rest/Soap/etc.) or mode name (RoslynWorkspace). Null means no change.</summary>
    public string? Protocol { get; set; }

    /// <summary>Gets or sets the timeout in seconds (Http connections). Null means no change.</summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets request headers to set on this Http connection. Null means no change; a supplied
    /// set is merged into the existing headers rather than replacing them.
    /// </summary>
    public IDictionary<string, string?>? Headers { get; set; }

    /// <summary>Gets or sets the security type discriminator (Http connections). Null means no change.</summary>
    public string? SecurityType { get; set; }

    /// <summary>Gets or sets the security key-value properties (Http connections). Null means no change; empty dictionary clears all values.</summary>
    public IDictionary<string, string?>? Security { get; set; }

    /// <summary>Gets or sets whether to use mutual TLS (Http connections). Null means no change.</summary>
    public bool? UseMtls { get; set; }


    /// <summary>Gets or sets whether the automated Connections domain health check probes this connection. Null means no change.</summary>
    public bool? HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection should be probed once at host startup. Null means no change.</summary>
    public bool? HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets the interval, in seconds, between periodic health checks. Null means no change.</summary>
    public int? HealthCheckIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the name of the schema discovery option the connection uses; it must be one its
    /// connection type offers. Null means no change.
    /// </summary>
    public string? SchemaDiscovery { get; set; }

    /// <summary>
    /// Gets or sets the named default-value policy this connection resolves through
    /// <c>DefaultValues.ByName(ServiceType).ByName(...)</c>. Null means no change.
    /// </summary>
    public string? DefaultValuePolicy { get; set; }

    /// <summary>
    /// Gets or sets the changes to this connection type's own settings: the members to change, shaped like
    /// the type's configuration class. Merged into what is stored -- nested objects and dictionaries member
    /// by member -- and a value equal to the masked placeholder keeps the stored value. Null means no change.
    /// </summary>
    /// <remarks>
    /// Preferred over the flat fields on this request, which are deprecated and still accepted as aliases;
    /// a member sent here wins over its flat equivalent. They will be removed.
    /// </remarks>
    public JsonElement? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the values of the connection's credentials, keyed by the credential fields its authentication
    /// method uses (for example <c>SecretKeyName</c> for a password, <c>ApiKeySecretName</c> for an API key).
    /// </summary>
    /// <remarks>
    /// Write-only. The server stores each value in the platform's credential secret manager under a secret it chooses
    /// and points the connection at it; no response, log line or endpoint ever returns a value, and a caller never
    /// names the secret manager or the secret (FDW-1086). Null keeps the stored credentials; a supplied set replaces them, and must name every credential the method uses.
    /// </remarks>
    public IDictionary<string, string>? Credentials { get; set; }
}
