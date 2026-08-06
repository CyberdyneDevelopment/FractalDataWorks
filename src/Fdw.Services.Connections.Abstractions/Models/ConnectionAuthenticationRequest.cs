namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Authentication property bag for connection requests.
/// Auth type discriminator lives on <see cref="CreateConnectionClientRequest.AuthenticationType"/>.
/// </summary>
// Why: pure request DTO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ConnectionAuthenticationRequest
{
    /// <summary>Gets or sets the username (for SQL authentication).</summary>
    public string? Username { get; set; }
    /// <summary>Gets or sets the secret key name for the password.</summary>
    public string? SecretKeyName { get; set; }
}
