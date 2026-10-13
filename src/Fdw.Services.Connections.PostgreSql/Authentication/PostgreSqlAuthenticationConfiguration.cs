using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Connections.PostgreSql.Authentication;

/// <summary>
/// Base class for PostgreSql authentication TypeOptions. Each concrete type
/// (None, Password) is a singleton behavior carrier — no mutable state, no prototype/clone.
/// Values are passed in as an <see cref="IReadOnlyDictionary{TKey,TValue}"/>
/// loaded from the <c>conn.PostgreSqlConnectionAuthentication</c> KVP table.
/// </summary>
public abstract class PostgreSqlAuthenticationConfiguration
    : ConnectionAuthenticationTypeBase<PostgreSqlAuthenticationConfiguration>
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected PostgreSqlAuthenticationConfiguration()
        : base(string.Empty, string.Empty, string.Empty, string.Empty, null, requiresSecret: false)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    protected PostgreSqlAuthenticationConfiguration(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties,
        bool requiresSecret)
        : base(name, $"Authentication:{name}", displayName, description, "Authentication", requiresSecret)
    {
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <inheritdoc />
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <inheritdoc />
    public abstract IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword);

    /// <inheritdoc />
    /// <remarks>
    /// Takes both a manager that may have been supplied directly and the provider that resolves one
    /// by name, because which of the two applies is this type's own decision: SecretManagerName is
    /// one of its properties. A type with no secret-bearing properties ignores both arguments.
    /// </remarks>
    /// <summary>Reports the secret this authentication type needs fetched before it can build.</summary>
    /// <param name="values">The connection's authentication properties.</param>
    /// <returns>
    /// The manager and key to read, or <see cref="SecretRequirement.None"/> when this type needs no
    /// secret — the default, since most authentication types carry no password.
    /// </returns>
    /// <remarks>
    /// This type decides; the provider fetches and hands the value back to
    /// <c>BuildAuthFragment(values, resolvedPassword)</c>, which is where the secret portion of the
    /// connection string is built. Deciding stays here, awaiting moved out.
    /// </remarks>
    public virtual IGenericResult<SecretRequirement> RequiredSecret(IReadOnlyDictionary<string, string?> values)
        => GenericResult<SecretRequirement>.Success(SecretRequirement.None);

    /// <inheritdoc />
    public abstract IGenericResult Validate(IReadOnlyDictionary<string, string?> values);
}
