namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// HTTP client request for updating an existing connection.
/// </summary>
/// <remarks>
/// Distinct from <c>Fdw.Services.Connections.Endpoints.UpdateConnectionRequest</c>,
/// which is the server-side endpoint contract.
/// </remarks>
public sealed class UpdateConnectionClientRequest
{
    // ── Routing discriminator ──────────────────────────────────────────────

    /// <summary>Gets or sets the service type (e.g. "Http", "PostgreSql"). Used by the connection API client to route to the correct typed endpoint.</summary>
    public string? ServiceType { get; set; }

    // ── MsSql / PostgreSql / common fields ────────────────────────────────

    /// <summary>Gets or sets the server hostname or IP address.</summary>
    public string? Server { get; set; }
    /// <summary>Gets or sets the port number.</summary>
    public int? Port { get; set; }
    /// <summary>Gets or sets the database name.</summary>
    public string? Database { get; set; }
    /// <summary>Gets or sets the authentication type discriminator.</summary>
    public string? AuthenticationType { get; set; }
    /// <summary>Gets or sets the authentication property bag (auth-type-specific fields, no type discriminator).</summary>
    public ConnectionAuthenticationRequest? Authentication { get; set; }
    /// <summary>Gets or sets whether to trust the server certificate.</summary>
    public bool? TrustServerCertificate { get; set; }
    /// <summary>Gets or sets whether to encrypt the connection.</summary>
    public bool? Encrypt { get; set; }
    /// <summary>Gets or sets whether the connection is active.</summary>
    public bool? IsActive { get; set; }

    // ── Http / FileSystem / RoslynWorkspace fields ────────────────────────

    /// <summary>Gets or sets the base URL (Http), root path (FileSystem), or solution path (RoslynWorkspace).</summary>
    public string? BaseUrl { get; set; }
    /// <summary>Gets or sets the protocol type (Http: Rest/Soap/etc.) or mode name (RoslynWorkspace).</summary>
    public string? Protocol { get; set; }
    /// <summary>Gets or sets the timeout in seconds (Http connections).</summary>
    public int? TimeoutSeconds { get; set; }
    /// <summary>Gets or sets the security type discriminator (Http connections).</summary>
    public string? SecurityType { get; set; }
    /// <summary>Gets or sets the security key-value properties (Http connections). Null means no change; empty dictionary clears all values.</summary>
    public System.Collections.Generic.IDictionary<string, string?>? Security { get; set; }
    /// <summary>Gets or sets whether to use mutual TLS (Http connections).</summary>
    public bool? UseMtls { get; set; }
}
