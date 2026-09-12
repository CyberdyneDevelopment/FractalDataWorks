using Fdw.Services.Connections.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Connections.Sqlite.Authentication;

/// <summary>
/// Base class for SQLite authentication TypeOptions. Each concrete type (None, EncryptionKey) is a
/// stateless behavior carrier that parses the connection's authentication KVP — loaded from
/// <c>conn.SqliteConnectionAuthentication</c> into a dictionary — however that method needs, and
/// resolves the optional encryption-key password.
/// </summary>
/// <remarks>
/// Mirrors <c>MsSqlAuthenticationConfiguration</c>. SQLite has no username/password login — its only
/// credential surface is the optional database-file encryption key (SQLCipher/SEE), injected as the
/// <c>Password=</c> connection-string keyword. So the auth-method output here is a resolved password
/// string (or null for None), not a connection-string fragment.
/// </remarks>
public abstract class SqliteAuthenticationConfiguration
    : TypeOptionBase<int, SqliteAuthenticationConfiguration>
{
    /// <summary>Parameterless constructor for the Empty/NotFound sentinel (source-generated).</summary>
    protected SqliteAuthenticationConfiguration()
        : base(0, string.Empty)
    {
    }

    /// <summary>Constructor for concrete TypeOptions.</summary>
    protected SqliteAuthenticationConfiguration(
        int id,
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties)
        : base(id, name, $"Authentication:{name}", displayName, description, "Authentication")
    {
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
    }

    /// <summary>The KVP keys this authentication method reads from its auth dictionary.</summary>
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <summary>The subset of <see cref="ExpectedProperties"/> that must be present.</summary>
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <summary>True for the Empty/NotFound sentinel.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>Validates the auth KVP for this method.</summary>
    public abstract IGenericResult Validate(IReadOnlyDictionary<string, string?> values);

    /// <summary>
    /// Reports the secret this authentication method needs fetched before a connection can be built.
    /// </summary>
    /// <param name="values">The connection's authentication properties.</param>
    /// <returns>
    /// The manager and key to read, or <see cref="SecretRequirement.None"/> for a method that needs
    /// none (None). A secret-using method (EncryptionKey) names its own
    /// <c>SecretManagerName</c>/<c>SecretKeyName</c>, because those are its properties.
    /// </returns>
    /// <remarks>
    /// The method decides what it needs; the implementation provider fetches it and hands the value
    /// back. The auth method never resolves a manager or reads a store itself, which is what lets
    /// the factory below it stay synchronous.
    /// </remarks>
    public abstract IGenericResult<SecretRequirement> RequiredSecret(
        IReadOnlyDictionary<string, string?> values);
}
