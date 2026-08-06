using System;
using System.Collections.Generic;

namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Detailed connection response from API.
/// </summary>
public sealed class ConnectionDetailResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the connection name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the service type.</summary>
    public string ServiceType { get; set; } = string.Empty;
    /// <summary>Gets or sets the server hostname.</summary>
    public string Server { get; set; } = string.Empty;
    /// <summary>Gets or sets the port number.</summary>
    public int Port { get; set; }
    /// <summary>Gets or sets the database name.</summary>
    public string Database { get; set; } = string.Empty;
    /// <summary>Gets or sets the authentication type.</summary>
    public string? AuthenticationType { get; set; }

    /// <summary>
    /// Gets or sets the authentication property bag returned by the server.
    /// Why: the server nests auth fields under <c>authentication</c> (e.g. <c>authentication.Username</c>);
    /// reading a flat <c>Username</c> always deserialized to null. <see cref="Username"/> projects out of this bag.
    /// </summary>
    public IDictionary<string, string?> Authentication { get; set; }
        = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the username, projected from the nested <see cref="Authentication"/> bag.
    /// Why: get-only (no setter) so STJ never deserializes a flat <c>username</c> over it; this DTO
    /// is response-only and is never serialized by the client, so no JsonIgnore is required.
    /// </summary>
    public string? Username =>
        Authentication.TryGetValue("Username", out var username) ? username : null;
    /// <summary>Gets or sets whether to trust the server certificate.</summary>
    public bool TrustServerCertificate { get; set; }
    /// <summary>Gets or sets whether the connection is encrypted.</summary>
    public bool Encrypt { get; set; }
    /// <summary>Gets or sets whether the connection is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets when the connection was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets when the connection was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>Gets or sets the connection setup summary, populated when auto-discover is available.</summary>
    public ConnectionSetupSummaryPayload? SetupSummary { get; set; }
}
