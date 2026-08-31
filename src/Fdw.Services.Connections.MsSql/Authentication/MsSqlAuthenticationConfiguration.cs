using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.Results;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.Connections.MsSql.Authentication;

/// <summary>
/// Base class for MsSql authentication TypeOptions. Each concrete type
/// (SqlAuth, WindowsAuth, EntraId, ManagedIdentity, AzureCli) is a singleton
/// behavior carrier — no mutable state, no prototype/clone.
/// Values are passed in as an <see cref="IReadOnlyDictionary{TKey,TValue}"/>
/// loaded from the <c>conn.MsSqlConnectionAuthentication</c> KVP table.
/// </summary>
public abstract class MsSqlAuthenticationConfiguration
    : TypeOptionBase<int, MsSqlAuthenticationConfiguration>
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected MsSqlAuthenticationConfiguration()
        : base(0, string.Empty)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    protected MsSqlAuthenticationConfiguration(
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
    /// surfaced in an API response. Empty for methods with no secret-bearing property
    /// (Windows, EntraId, ManagedIdentity, AzureCli).
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

    /// <summary>
    /// Gets a value indicating whether this authentication type needs a secret resolved before a
    /// connection can be built.
    /// </summary>
    /// <summary>
    /// Reads one authentication property, failing loud when it is absent or empty.
    /// </summary>
    /// <param name="values">The authentication KVP values for this connection.</param>
    /// <param name="name">The property name to read.</param>
    /// <returns>The value, or a structured failure naming the property and this authentication type.</returns>
    public IGenericResult<string> GetValue(IReadOnlyDictionary<string, string?> values, string name)
        => values is not null && values.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value)
            ? GenericResult<string>.Success(value)
            : GenericResult<string>.Failure(
                MsSqlConnectionResultCodes.ByName("AuthenticationValueMissing"),
                ResultDetails.Create("Property", name, "AuthenticationType", Name));

    /// <inheritdoc />
    public virtual bool UsesAccessToken => false;

    /// <inheritdoc />
    public virtual string? AcquireAccessToken() => null;

    /// <inheritdoc />
    public abstract IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword);

    /// <inheritdoc />
    /// <summary>Builds the authentication fragment, resolving any secret this type needs.</summary>
    /// <param name="values">The connection's authentication properties.</param>
    /// <param name="supplied">
    /// A manager handed in directly, by a caller that cannot resolve one by name — the connection
    /// that reaches the configuration store is in that position, since the provider that would
    /// resolve it reads its own configuration out of the store being opened.
    /// </param>
    /// <param name="provider">Resolves a manager by the name this type's own properties declare.</param>
    /// <param name="cancellationToken">A token to cancel the resolution.</param>
    /// <remarks>
    /// Whether a secret is needed, which manager holds it, and how to fetch it are all decided here,
    /// because this type owns the property set those answers come from — SecretManagerName and
    /// SecretKeyName are its keys, not the factory's. The default needs none: a type that declares no
    /// secret-bearing properties ignores both arguments.
    /// </remarks>
    public virtual Task<IGenericResult<string>> BuildAuthFragment(
        IReadOnlyDictionary<string, string?> values,
        ISecretManager? supplied,
        ISecretManagerProvider? provider,
        CancellationToken cancellationToken = default)
        => Task.FromResult(BuildAuthFragment(values, resolvedPassword: null));

    /// <inheritdoc />
    public abstract IGenericResult Validate(IReadOnlyDictionary<string, string?> values);
}
