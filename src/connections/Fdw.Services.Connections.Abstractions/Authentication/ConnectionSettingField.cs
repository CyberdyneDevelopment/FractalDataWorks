using System;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// One non-secret value an authentication method requires, and how a person is asked for it (FDW-1336).
/// </summary>
/// <remarks>
/// Declared by each authentication method on <see cref="ConnectionAuthenticationTypeBase{TSelf}.Settings"/>: the SQL login
/// name, for example. It is an authentication value like a credential's, but it is stored as typed and shown as typed,
/// where a credential is stored in a secret manager and never read back (FDW-1086). The label is part of the declaration,
/// never derived from <see cref="Field"/>.
/// </remarks>
public sealed class ConnectionSettingField
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionSettingField"/> class.</summary>
    /// <param name="field">The authentication value that holds the setting (for example <c>Username</c>).</param>
    /// <param name="label">What a person is asked for (for example "User name").</param>
    public ConnectionSettingField(string field, string label)
    {
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("A setting field is required.", nameof(field));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A setting label is required.", nameof(label));

        Field = field;
        Label = label;
    }

    /// <summary>Gets the authentication value that holds the setting.</summary>
    public string Field { get; }

    /// <summary>Gets what a person is asked for.</summary>
    public string Label { get; }
}
