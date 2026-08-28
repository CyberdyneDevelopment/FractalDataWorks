namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// HTTP client request for creating a new connection.
/// </summary>
/// <remarks>
/// Distinct from <c>Fdw.Services.Connections.Endpoints.CreateConnectionRequest</c>,
/// which is the server-side endpoint contract (FluentValidation + ResourceCreateRequest base).
/// </remarks>
public sealed class CreateConnectionClientRequest
{
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the service type (e.g., MsSql, PostgreSql).</summary>
    public string ServiceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the server hostname or IP address.</summary>
    public string Server { get; set; } = string.Empty;
    /// <summary>Gets or sets the port number.</summary>
    public int Port { get; set; } = 1433;
    /// <summary>Gets or sets the database name.</summary>
    public string Database { get; set; } = string.Empty;
    /// <summary>Gets or sets the authentication type discriminator (e.g., "SqlAuth", "WindowsAuth").</summary>
    public string? AuthenticationType { get; set; }
    /// <summary>Gets or sets the authentication property bag (auth-type-specific fields, no type discriminator).</summary>
    public ConnectionAuthenticationRequest? Authentication { get; set; }
    /// <summary>Gets or sets whether to trust the server certificate.</summary>
    public bool TrustServerCertificate { get; set; }
    /// <summary>Gets or sets whether to encrypt the connection.</summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>Gets or sets the base URL for HTTP connections (e.g., "https://api.example.com/v1/").</summary>
    public string? BaseUrl { get; set; }
    /// <summary>Gets or sets the protocol type: Rest, Soap11, Soap12, GraphQL, OData.</summary>
    public string? Protocol { get; set; }
    /// <summary>Gets or sets the request timeout in seconds.</summary>
    public int? TimeoutSeconds { get; set; }
    /// <summary>Gets or sets the security type: None, BasicAuth, ApiKey, WsSecurity, etc.</summary>
    public string? SecurityType { get; set; }
    /// <summary>Gets or sets the security configuration key-value pairs (API key, credentials, etc.).</summary>
    public System.Collections.Generic.IDictionary<string, string?>? Security { get; set; }
    /// <summary>Gets or sets whether to use mutual TLS (client certificate).</summary>
    public bool UseMtls { get; set; }
}
