using System;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// One stored credential an authentication method uses: the authentication value that names it, and how a person
/// is asked for it.
/// </summary>
/// <remarks>
/// Declared by each authentication method on <see cref="ConnectionAuthenticationTypeBase{TSelf}.Credentials"/>. The
/// label is part of the declaration, never derived from <see cref="Field"/>, so a form can ask for "Password" rather
/// than "SecretKeyName" (FDW-1090). A caller sets the value through the connection and never reads it back (FDW-1086).
/// </remarks>
public sealed class ConnectionCredentialField
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionCredentialField"/> class.</summary>
    /// <param name="field">The authentication value that names the stored credential (for example <c>SecretKeyName</c>).</param>
    /// <param name="label">What a person is asked for (for example "Password").</param>
    /// <param name="secret">Whether the value is entered hidden, as a password is; a user name is not.</param>
    public ConnectionCredentialField(string field, string label, bool secret)
    {
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("A credential field is required.", nameof(field));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A credential label is required.", nameof(label));

        Field = field;
        Label = label;
        Secret = secret;
    }

    /// <summary>Gets the authentication value that names the stored credential.</summary>
    public string Field { get; }

    /// <summary>Gets what a person is asked for.</summary>
    public string Label { get; }

    /// <summary>Gets a value indicating whether the value is entered hidden.</summary>
    public bool Secret { get; }
}
