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
}
