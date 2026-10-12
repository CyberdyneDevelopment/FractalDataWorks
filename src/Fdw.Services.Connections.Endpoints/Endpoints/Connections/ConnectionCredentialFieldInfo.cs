namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// One credential an authentication method asks for, as a create or edit form shows it.
/// </summary>
/// <remarks>Never a value: the field is set write-only through the connection (FDW-1086).</remarks>
public sealed class ConnectionCredentialFieldInfo
{
    /// <summary>Gets or sets the credential field the connection's <c>credentials</c> object is keyed by (for example <c>SecretKeyName</c>).</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets what a person is asked for, as the authentication method declares it (for example "Password").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the credential must be supplied. Every credential a method declares is
    /// required: a save sets all of the method's credentials or keeps the ones already stored.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>Gets or sets a value indicating whether the value is entered hidden, as a password is.</summary>
    public bool Secret { get; set; }
}
