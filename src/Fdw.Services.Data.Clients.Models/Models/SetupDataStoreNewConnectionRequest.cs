using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Inline connection configuration for creating a new connection as part of DataStore setup.
/// Field names match <c>Fdw.Services.Connections.Endpoints.CreateConnectionRequest</c>
/// for correct JSON deserialization on the server.
/// </summary>
public sealed class SetupDataStoreNewConnectionRequest
{
    /// <summary>Gets or sets the connection name. Defaults to the DataStore name if omitted.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the service type (e.g., "MsSql").</summary>
    public string ServiceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the server hostname or IP address.</summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>Gets or sets the server port number.</summary>
    public int Port { get; set; } = 1433;

    /// <summary>Gets or sets the database name.</summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication type discriminator (e.g., "Default", "SqlAuth").</summary>
    public string? AuthenticationType { get; set; }

    /// <summary>
    /// Gets or sets authentication key-value pairs (e.g., Username, SecretKeyName).
    /// Keys are case-insensitive on the server.
    /// </summary>
    public IDictionary<string, string?> Authentication { get; set; }
        = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets whether to trust the server certificate.</summary>
    public bool TrustServerCertificate { get; set; }

    /// <summary>Gets or sets whether to encrypt the connection.</summary>
    public bool Encrypt { get; set; } = true;
}
