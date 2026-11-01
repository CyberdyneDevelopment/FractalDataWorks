using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions.Authentication;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// Base class for HTTP security configurations.
/// Each concrete type (None, WsSecurity, etc.) extends this with its own
/// bindable properties.
///
/// The TypeCollection holds one singleton prototype per type. Call CreateInstance()
/// on the prototype to get a fresh instance whose properties can be bound
/// from IConfiguration. The identity properties (Id, Name, etc.) are set by
/// the constructor and are NOT bindable.
/// </summary>
/// <remarks>
/// Implements the Abstractions HttpAuthenticationConfiguration directly (rather than through the
/// local HttpAuthenticationConfiguration) to avoid Name ambiguity between ITypeOption.Name and
/// Abstractions.HttpAuthenticationConfiguration.Name in source-generated TypeCollection code.
/// </remarks>
public abstract class HttpAuthenticationConfiguration
    : ConnectionAuthenticationTypeBase<HttpAuthenticationConfiguration>
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected HttpAuthenticationConfiguration()
        : base(string.Empty, string.Empty, string.Empty, string.Empty, null, requiresSecret: false)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions with no type-specific properties.
    /// </summary>
    protected HttpAuthenticationConfiguration(
        string name,
        string displayName,
        string description,
        bool requiresSecret)
        : base(name, $"Security:{name}", displayName, description, "Security", requiresSecret)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions with type-specific property metadata.
    /// </summary>
    protected HttpAuthenticationConfiguration(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties,
        bool requiresSecret)
        : base(name, $"Security:{name}", displayName, description, "Security", requiresSecret)
    {
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <summary>
    /// Gets the security type discriminator. Derived from the TypeOption Name.
    /// </summary>
    [ValuesFrom(typeof(HttpAuthenticationTypes))]
    public string Type => Name;

    /// <inheritdoc />
    public bool IsEmpty => string.IsNullOrEmpty(Name);


    /// <inheritdoc />
    public abstract HttpAuthenticationConfiguration CreateInstance();

    /// <summary>
    /// Gets the names of the secrets this method reads, by the slot of the protocol context each travels in.
    /// <see cref="HttpSecretNames.None"/> for a method that reads none.
    /// </summary>
    /// <remarks>
    /// Abstract so every method states it. The connection factory asks the bound instance and keeps no list of methods, so a method that
    /// reads a secret and did not say so would be built with no credential, and the server's 401 would be the first sign (FDW-2844).
    /// </remarks>
    public abstract HttpSecretNames SecretNames { get; }

    /// <summary>
    /// Binds this instance's settable properties from an IConfigurationSection.
    /// Base implementation is a no-op — there are no common properties across all security types.
    /// Each concrete type overrides to bind its own properties.
    /// </summary>
    public virtual void Bind(IConfigurationSection section)
    {
    }

    /// <summary>
    /// Decomposes this instance into key-value pairs for writing to the KVP table.
    /// Base implementation emits only the Type discriminator.
    /// Each concrete type overrides to add its own properties.
    /// </summary>
    public virtual IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return [new("Type", Name)];
    }

    /// <summary>
    /// Hydrates this instance's method-specific keys from the connection's authentication KVP.
    /// Counterpart to <see cref="AsKvp"/>. Base implementation is a no-op. Each secret-backed method
    /// overrides to read its own keys (SecretManagerName plus its secret-name keys); methods that need
    /// no secret manager (managed-identity, integrated, MFA, none) leave this a no-op.
    /// </summary>
    public virtual void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
    }
}
