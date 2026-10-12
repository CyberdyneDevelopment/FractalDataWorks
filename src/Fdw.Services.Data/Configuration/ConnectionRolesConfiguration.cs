namespace Fdw.Services.Data.Configuration;

/// <summary>
/// The connection each role plays in one host, as the host's <c>configurationSchema.json</c> declares it:
/// <c>"ConnectionRoles": { "Platform": "...", "Server": "...", "Auth": "..." }</c>.
/// </summary>
/// <remarks>
/// Each value is the <c>Name</c> of a connection the same file declares under <c>Connections</c>. The platform and server
/// roles are required of every host. The auth role is declared by a host that authenticates; a host that has no
/// authentication connection leaves it out, and anything that then asks for it fails loud instead of being given a name.
/// </remarks>
public sealed class ConnectionRolesConfiguration
{
    /// <summary>Gets or sets the connection holding the platform's own configuration rows.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets the connection holding boot-time, near-static server configuration.</summary>
    public string? Server { get; set; }

    /// <summary>Gets or sets the connection holding sessions, flow state and token revocation.</summary>
    public string? Auth { get; set; }
}
