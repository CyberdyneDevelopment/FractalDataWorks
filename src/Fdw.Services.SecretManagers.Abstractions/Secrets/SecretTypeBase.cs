using System;
using Fdw.Collections;

namespace Fdw.Services.SecretManagers.Abstractions.Secrets;

/// <summary>
/// Base class for secret type markers used to identify what type of secret credential a service requires.
/// </summary>
/// <remarks>
/// Secret types are marker classes that represent different kinds of credentials
/// (e.g., None, String, OAuth2, Certificate, ConnectionString, ApiKey).
/// Use typeof(SecretType) to specify what type of secret a service needs.
/// </remarks>
public abstract class SecretTypeBase : TypeOptionBase<int, ISecretType>, ISecretType
{
    /// <summary>
    /// Gets a value indicating whether this secret type requires secure storage.
    /// </summary>
    public bool RequiresSecureStorage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this secret type.</param>
    /// <param name="name">The name of this secret type.</param>
    /// <param name="description">The description of this secret type.</param>
    /// <param name="requiresSecureStorage">Indicates whether this secret type requires secure storage.</param>
    /// <param name="category">The category for this secret type (defaults to "Secret").</param>
    protected SecretTypeBase(
        int id,
        string name,
        string description,
        bool requiresSecureStorage = true,
        string? category = null)
        : base(id, name, name, name, description ?? string.Empty, category ?? "Secret")
    {
        RequiresSecureStorage = requiresSecureStorage;
    }
}
