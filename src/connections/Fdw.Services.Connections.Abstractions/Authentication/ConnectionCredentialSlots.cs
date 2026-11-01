using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Connections.Abstractions.Authentication;

/// <summary>
/// The stored credentials a connection's authentication method uses, and which of them the connection has.
/// </summary>
/// <remarks>
/// <para>
/// A credential belongs to one connection. A caller sets its value through the connection and never reads it back;
/// the server chooses the secret manager and the secret each <see cref="Fields">field</see> names, and resolves the
/// value only when the connection is used (FDW-1086). This type therefore carries names, never values.
/// </para>
/// <para>
/// <see cref="Credentials"/> come from the authentication method's own
/// <see cref="ConnectionAuthenticationTypeBase{TSelf}.Credentials"/>, labels included; <see cref="References"/> are
/// what the connection's authentication values currently name for each of them.
/// </para>
/// </remarks>
public sealed class ConnectionCredentialSlots
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionCredentialSlots"/> class.</summary>
    /// <param name="method">The authentication method the connection names; empty for a kind with none.</param>
    /// <param name="credentials">The credentials the authentication method uses, as it declares them.</param>
    /// <param name="secretManagerName">The secret manager the connection's credentials are stored in, if it names one.</param>
    /// <param name="references">The secret each field names, or <see langword="null"/> for a field that names none.</param>
    public ConnectionCredentialSlots(
        string method,
        IReadOnlyList<ConnectionCredentialField> credentials,
        string? secretManagerName,
        IReadOnlyDictionary<string, string?> references)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        Fields = credentials.Select(credential => credential.Field).ToList();
        SecretManagerName = string.IsNullOrEmpty(secretManagerName) ? null : secretManagerName;
        References = references ?? throw new ArgumentNullException(nameof(references));
    }

    /// <summary>Gets the slots of a connection whose authentication carries no stored credential.</summary>
    public static ConnectionCredentialSlots None { get; } =
        new(string.Empty, [], null, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Gets the authentication method the connection names; empty for a kind that has none.</summary>
    public string Method { get; }

    /// <summary>Gets the credentials the connection's authentication method uses, as it declares them.</summary>
    public IReadOnlyList<ConnectionCredentialField> Credentials { get; }

    /// <summary>Gets the credential fields the connection's authentication method uses: the <see cref="Credentials"/>' fields.</summary>
    public IReadOnlyList<string> Fields { get; }

    /// <summary>Gets the secret manager the connection's credentials are stored in, or <see langword="null"/> when it names none.</summary>
    public string? SecretManagerName { get; }

    /// <summary>Gets the secret each credential field names; a field that names none maps to <see langword="null"/>.</summary>
    public IReadOnlyDictionary<string, string?> References { get; }

    /// <summary>Gets the fields that have a stored credential.</summary>
    public IReadOnlyList<string> SetFields => Fields.Where(IsSet).ToList();

    /// <summary>Gets the fields that have no stored credential yet.</summary>
    public IReadOnlyList<string> MissingFields => Fields.Where(credentialField => !IsSet(credentialField)).ToList();

    /// <summary>Whether <paramref name="field"/> names a stored credential.</summary>
    /// <param name="field">A credential field.</param>
    /// <returns><see langword="true"/> when the connection names both a secret manager and a secret for the field.</returns>
    public bool IsSet(string field)
        => SecretManagerName is not null
           && References.TryGetValue(field, out var reference)
           && !string.IsNullOrEmpty(reference);

    /// <summary>
    /// Reads the slots from a connection's authentication values.
    /// </summary>
    /// <param name="method">The authentication method the connection names.</param>
    /// <param name="credentials">The credentials the connection's authentication method uses, as it declares them.</param>
    /// <param name="values">The connection's authentication values.</param>
    /// <returns>The slots, naming what the values currently reference.</returns>
    public static ConnectionCredentialSlots From(
        string method, IReadOnlyList<ConnectionCredentialField> credentials, IDictionary<string, string?> values)
    {
        if (credentials is null) throw new ArgumentNullException(nameof(credentials));
        if (values is null) throw new ArgumentNullException(nameof(values));

        var lookup = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        var references = credentials.ToDictionary(
            credential => credential.Field,
            credential => lookup.TryGetValue(credential.Field, out var reference) ? reference : null,
            StringComparer.OrdinalIgnoreCase);
        lookup.TryGetValue(ConnectionCredentialProperties.SecretManager, out var manager);
        return new ConnectionCredentialSlots(method, credentials, manager, references);
    }

    /// <summary>
    /// Points a connection's authentication values at the secrets the server stored for it.
    /// </summary>
    /// <param name="values">The connection's authentication values, changed in place.</param>
    /// <param name="secretManagerName">The secret manager the credentials were stored in.</param>
    /// <param name="secretKeyByField">The secret stored for each credential field.</param>
    public static void Bind(
        IDictionary<string, string?> values,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField)
    {
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (string.IsNullOrEmpty(secretManagerName)) throw new ArgumentException("A secret manager name is required.", nameof(secretManagerName));
        if (secretKeyByField is null) throw new ArgumentNullException(nameof(secretKeyByField));

        values[ConnectionCredentialProperties.SecretManager] = secretManagerName;
        foreach (var binding in secretKeyByField)
            values[binding.Key] = binding.Value;
    }
}
