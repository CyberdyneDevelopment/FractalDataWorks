using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
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
    : ConnectionAuthenticationTypeBase<MsSqlAuthenticationConfiguration>
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected MsSqlAuthenticationConfiguration()
        : base(string.Empty, string.Empty, string.Empty, string.Empty, null, requiresSecret: false)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    protected MsSqlAuthenticationConfiguration(
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
    public abstract IGenericResult<string> BuildAuthFragment(IReadOnlyDictionary<string, string?> values, string? resolvedPassword);

    /// <summary>Reports the secret this authentication type needs fetched before it can build.</summary>
    /// <param name="values">The connection's authentication properties.</param>
    /// <returns>
    /// The manager and key to read, or <see cref="SecretRequirement.None"/> when this type needs no
    /// secret — which is the default, since most authentication types carry no password.
    /// </returns>
    /// <remarks>
    /// Whether a secret is needed, which manager holds it and under which key are all decided here,
    /// because this type owns the property set those answers come from — SecretManagerName and
    /// SecretKeyName are its keys, not the factory's. What changed is only that this type no longer
    /// goes and FETCHES it: the provider does that and hands the value back to
    /// <see cref="BuildAuthFragment(IReadOnlyDictionary{string, string}, string)"/>, which is where
    /// this type builds the secret portion of the connection string. Deciding stays here; awaiting
    /// moved out, so a factory never has to be asynchronous.
    /// </remarks>
    public virtual IGenericResult<SecretRequirement> RequiredSecret(IReadOnlyDictionary<string, string?> values)
        => GenericResult<SecretRequirement>.Success(SecretRequirement.None);

    /// <inheritdoc />
    public abstract IGenericResult Validate(IReadOnlyDictionary<string, string?> values);
}
