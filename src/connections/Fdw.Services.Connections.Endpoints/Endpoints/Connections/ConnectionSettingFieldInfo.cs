namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// One non-secret value an authentication method requires, as a form asks for it (FDW-1336).
/// </summary>
/// <remarks>
/// Sent in the connection's <c>authentication</c> under <see cref="Field"/>, and shown as typed: it is a setting, not a
/// credential.
/// </remarks>
public sealed class ConnectionSettingFieldInfo
{
    /// <summary>Gets or sets the authentication value that holds the setting (for example <c>Username</c>).</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets what a person is asked for (for example "User name").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the method needs it; always true: a method lists only what it requires.</summary>
    public bool Required { get; set; }
}
