namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Whether a connection has a stored credential for one of the credential fields its authentication method uses.
/// </summary>
/// <remarks>
/// Only that a credential is set: never its value, and never the secret it is stored in (FDW-1086).
/// </remarks>
public sealed class ConnectionCredentialStatus
{
    /// <summary>Gets or sets the credential field (for example <c>SecretKeyName</c> for a password).</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets what a person is asked for, as the authentication method declares it (for example "Password").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the value is entered hidden, as a password is.</summary>
    public bool Secret { get; set; }

    /// <summary>Gets or sets a value indicating whether the connection has a stored credential for the field.</summary>
    public bool IsSet { get; set; }
}
