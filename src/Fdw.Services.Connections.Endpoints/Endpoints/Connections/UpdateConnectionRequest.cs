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

    // Why: mirrors the check-settings columns on conn.Connection (see CreateConnectionRequest).
    // Null means no change — consistent with every other nullable field on this DTO.

    /// <summary>Gets or sets whether the automated Connections domain health check probes this connection. Null means no change.</summary>
    public bool? HealthCheckEnabled { get; set; }

    /// <summary>Gets or sets whether this connection should be probed once at host startup. Null means no change.</summary>
    public bool? HealthCheckOnStartup { get; set; }

    /// <summary>Gets or sets the interval, in seconds, between periodic health checks. Null means no change.</summary>
    public int? HealthCheckIntervalSeconds { get; set; }
}
