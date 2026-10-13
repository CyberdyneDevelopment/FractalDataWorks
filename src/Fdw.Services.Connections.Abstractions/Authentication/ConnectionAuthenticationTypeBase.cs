using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// Shared base for a connection kind's authentication method TypeOptions (e.g.
/// MsSqlAuthenticationConfiguration, PostgreSqlAuthenticationConfiguration). Each connection kind
/// still owns its own TypeCollection and behavior -- this only carries the one property every
/// authentication method needs regardless of which connection it authenticates: whether opening a
/// connection with it needs a secret resolved first.
/// </summary>
public abstract class ConnectionAuthenticationTypeBase<TSelf> : TypeOptionBase<TSelf>
    where TSelf : ITypeOption<int, TSelf>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionAuthenticationTypeBase{TSelf}"/> class.
    /// </summary>
    /// <param name="name">The name of this authentication method (e.g. "SqlAuth", "ManagedIdentity").</param>
    /// <param name="configurationKey">The configuration key this method is bound under.</param>
    /// <param name="displayName">The display name for this authentication method.</param>
    /// <param name="description">The description for this authentication method.</param>
    /// <param name="category">The category of this authentication method.</param>
    /// <param name="requiresSecret">
    /// Whether opening a connection with this authentication method needs a secret resolved
    /// first (a password) -- true for credential-based methods (SqlAuth), false for every method
    /// whose identity comes from the platform itself (ManagedIdentity, EntraId, AzureCli,
    /// WindowsAuth). Set once per TypeOption in its own constructor; never computed at runtime.
    /// </param>
    protected ConnectionAuthenticationTypeBase(
        string name,
        string configurationKey,
        string displayName,
        string description,
        string? category,
        bool requiresSecret)
        : base(name, configurationKey, displayName, description, category)
    {
        RequiresSecret = requiresSecret;
    }

    /// <summary>
    /// Whether opening a connection with this authentication method needs a secret resolved
    /// first -- true for credential-based methods, false for methods whose identity comes from
    /// the platform itself.
    /// </summary>
    public bool RequiresSecret { get; }

    /// <summary>
    /// Whether a connection opened with this authentication method presents the host's own identity -- the
    /// account, Kerberos ticket, managed identity, CLI login or file-system access of the process the host runs
    /// as -- rather than a credential the connection itself carries.
    /// </summary>
    /// <param name="values">The connection's authentication properties, for methods whose answer depends on them.</param>
    /// <returns><see langword="true"/> when the connection acts as the host.</returns>
    /// <remarks>
    /// Every method states its answer; nothing assumes one. A connection that acts as the host reaches whatever the
    /// host's identity reaches, including the platform's own databases and files, so creating or changing one is a
    /// platform administrator's decision (FDW-1068). This is a separate fact from <see cref="RequiresSecret"/>: an
    /// HTTP connection with no security needs no secret and presents no identity at all.
    /// </remarks>
    public abstract bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values);

    /// <summary>
    /// Gets the stored credentials this method uses, one per credential (a password, an API key, a client
    /// certificate), each with the label a person is asked for. Empty for a method that carries no credential.
    /// </summary>
    /// <remarks>
    /// Abstract so every method states it; nothing infers a method's credentials or their labels from its property
    /// names (FDW-1090). The named values, and <see cref="ConnectionCredentialProperties.SecretManager"/>, are written by
    /// the server when a caller sets the connection's credentials: a caller never chooses which secret manager or which
    /// secret a connection reads, and never reads a credential back (FDW-1086).
    /// </remarks>
    public abstract IReadOnlyList<ConnectionCredentialField> Credentials { get; }

    /// <summary>
    /// Gets the authentication values that name this method's stored credentials: the <see cref="Credentials"/>' fields.
    /// </summary>
    public IReadOnlyList<string> CredentialFields => Credentials.Select(credential => credential.Field).ToList();

    /// <summary>
    /// Whether <paramref name="property"/> is one of the authentication values the server sets: a credential
    /// field or <see cref="ConnectionCredentialProperties.SecretManager"/>.
    /// </summary>
    /// <param name="property">An authentication value's name.</param>
    /// <returns><see langword="true"/> when only the server writes the value.</returns>
    public bool IsServerSet(string property)
        => string.Equals(property, ConnectionCredentialProperties.SecretManager, StringComparison.OrdinalIgnoreCase)
           || CredentialFields.Contains(property, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the non-secret values this method requires besides its credentials (the SQL login name of a password
    /// method), each with the label a person is asked for. Empty for a method that requires none (FDW-1336).
    /// </summary>
    /// <remarks>
    /// Stated per method, like <see cref="Credentials"/>, so a form can ask for every required value before the
    /// connection exists. Each is an authentication value: a caller sends it in <c>authentication</c> under its field name.
    /// </remarks>
    public virtual IReadOnlyList<ConnectionSettingField> Settings => [];

    /// <summary>
    /// Masks the stored-credential references in <paramref name="values"/>, replacing each non-empty one with
    /// <paramref name="maskValue"/> (FDW-1086). The one definition for every connection kind.
    /// </summary>
    /// <param name="values">The connection's authentication values.</param>
    /// <param name="maskValue">What a masked value is shown as.</param>
    /// <returns>The values with the credential references masked; the secret manager and the settings are shown as they are.</returns>
    /// <remarks>
    /// Why: a credential's value is the name of a secret, and a caller never learns where a credential is kept. The secret
    /// manager's name and the method's settings are not secret. An unrecognised method (the NotFound sentinel has no
    /// name) masks every non-empty value: it is treated as unsafe to show, not as "nothing declared, nothing to hide".
    /// </remarks>
    public IDictionary<string, string?> MaskSecrets(IReadOnlyDictionary<string, string?> values, string maskValue)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));

        // Copied entry by entry: this project targets netstandard2.0, where Dictionary has no constructor over an
        // IReadOnlyDictionary.
        var masked = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in values)
            masked[entry.Key] = entry.Value;

        var keysToMask = string.IsNullOrEmpty(Name) ? masked.Keys.ToList() : CredentialFields.ToList();
        foreach (var key in keysToMask)
        {
            if (masked.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                masked[key] = maskValue;
        }

        return masked;
    }
}
