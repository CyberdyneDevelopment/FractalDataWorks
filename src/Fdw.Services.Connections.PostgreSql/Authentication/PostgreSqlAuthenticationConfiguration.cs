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
    : TypeOptionBase<int, PostgreSqlAuthenticationConfiguration>
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected PostgreSqlAuthenticationConfiguration()
        : base(0, string.Empty)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    protected PostgreSqlAuthenticationConfiguration(
        int id,
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties,
        IReadOnlyList<string> secretPropertyNames)
        : base(id, name, $"Authentication:{name}", displayName, description, "Authentication")
    {
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
        SecretPropertyNames = secretPropertyNames;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <summary>
    /// The subset of <see cref="ExpectedProperties"/> whose values are secret references
    /// (e.g. <c>SecretKeyName</c>) and must be masked before this type's KVP values are
    /// surfaced in an API response. Empty for methods with no secret-bearing property (None).
    /// </summary>
    public IReadOnlyList<string> SecretPropertyNames { get; } = [];

    /// <inheritdoc />
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>
    /// Masks this type's secret-bearing properties (<see cref="SecretPropertyNames"/>) in
    /// <paramref name="values"/>, replacing each non-empty value with <paramref name="maskValue"/>.
    /// </summary>
    /// <remarks>
    /// Why: the NotFound/Empty sentinel (unrecognized or unset AuthenticationType) masks every
    /// non-empty value rather than none — an unrecognized type is treated as unsafe to display,
    /// not as "nothing declared, nothing to hide".
    /// </remarks>
    public IDictionary<string, string?> MaskSecrets(IReadOnlyDictionary<string, string?> values, string maskValue)
    {
        var masked = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> keysToMask = IsEmpty ? new List<string>(masked.Keys) : SecretPropertyNames;
        foreach (var key in keysToMask)
        {
            if (masked.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                masked[key] = maskValue;
        }

        return masked;
    }

    /// <inheritdoc />
    public abstract IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword);

    /// <inheritdoc />
    // Why: default implementation defers to the sync overload with no resolved password —
    // appropriate for auth types that don't need a secret (None). Types that need a secret
    // (Password) override and pull SecretKeyName from values themselves before calling
    // secretManager.Execute.
    public virtual Task<IGenericResult<string>> BuildAuthFragment(
        IReadOnlyDictionary<string, string?> values,
        ISecretManager? secretManager,
        CancellationToken cancellationToken = default)
        => Task.FromResult(BuildAuthFragment(values, resolvedPassword: null));

    /// <inheritdoc />
    public abstract IGenericResult Validate(IReadOnlyDictionary<string, string?> values);
}
