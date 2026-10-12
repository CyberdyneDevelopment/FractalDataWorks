namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// The authentication value names every kind of connection uses for its stored credentials.
/// </summary>
public static class ConnectionCredentialProperties
{
    /// <summary>
    /// The authentication value that names the secret manager holding a connection's stored credentials.
    /// </summary>
    /// <remarks>
    /// Set by the server when it stores a connection's credentials, never by a caller (FDW-1086).
    /// </remarks>
    public const string SecretManager = "SecretManagerName";
}
