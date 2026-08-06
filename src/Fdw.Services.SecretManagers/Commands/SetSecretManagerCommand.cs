using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ManagementCommand for storing or updating a secret value in a secret provider.
/// </summary>
/// <remarks>
/// SetSecretManagerCommand stores a new secret or updates an existing secret
/// in the configured secret store. The managementCommand can specify expiration dates,
/// tags, and other metadata through its parameters.
/// </remarks>
public sealed class SetSecretManagerCommand : SecretManagerCommandBase, ISecretManagerCommand<SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetSecretManagerCommand"/> class.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="secretValue">The secret value to store.</param>
    /// <param name="parameters">ManagementCommand parameters (e.g., ExpirationDate, Tags, Description).</param>
    /// <param name="metadata">Additional managementCommand metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <exception cref="ArgumentException">Thrown when secretKey is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when secretValue is null.</exception>
    public SetSecretManagerCommand(
        string? container,
        string secretKey,
        string secretValue,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null)
        : base("SetSecret", container, secretKey, typeof(SecretValue), CreateParametersWithValue(secretValue, parameters), metadata, timeout)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("Secret key cannot be null or empty for SetSecret operation.", nameof(secretKey));
    }

    /// <inheritdoc/>
    public override bool IsSecretModifying => true;

    /// <summary>
    /// Gets the secret value to store.
    /// </summary>
    /// <value>The secret value.</value>
    public string SecretValue => Parameters[nameof(SecretValue)]?.ToString() ?? string.Empty;

    /// <summary>
    /// Gets the expiration date for the secret.
    /// </summary>
    /// <value>The expiration date, or null if no expiration is set.</value>
    public DateTimeOffset? ExpirationDate => Parameters.TryGetValue(nameof(ExpirationDate), out var expiry) &&
                                             expiry is DateTimeOffset expirationDate ? expirationDate : null;

    /// <summary>
    /// Gets the description for the secret.
    /// </summary>
    /// <value>The description, or null if no description is set.</value>
    public string? Description => Parameters.TryGetValue(nameof(Description), out var desc) ? desc?.ToString() : null;

    /// <summary>
    /// Gets the tags for the secret.
    /// </summary>
    /// <value>A dictionary of tags, or empty dictionary if no tags are set.</value>
    public IReadOnlyDictionary<string, string> Tags
    {
        get
        {
            if (Parameters.TryGetValue(nameof(Tags), out var tagsObj) && tagsObj is IReadOnlyDictionary<string, string> tags)
            {
                return tags;
            }
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Creates a SetSecretManagerCommand with basic secret value.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="secretValue">The secret value to store.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new SetSecretManagerCommand instance.</returns>
    public static SetSecretManagerCommand Create(string? container, string secretKey, string secretValue, TimeSpan? timeout = null)
    {
        return new SetSecretManagerCommand(container, secretKey, secretValue, null, null, timeout);
    }

    /// <summary>
    /// Creates a SetSecretManagerCommand with description.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="secretValue">The secret value to store.</param>
    /// <param name="description">The description for the secret.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new SetSecretManagerCommand instance.</returns>
    public static SetSecretManagerCommand WithDescription(string? container, string secretKey, string secretValue, string description, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(Description)] = description
        };

        return new SetSecretManagerCommand(container, secretKey, secretValue, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a SetSecretManagerCommand with expiration date.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="secretValue">The secret value to store.</param>
    /// <param name="expirationDate">The expiration date for the secret.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new SetSecretManagerCommand instance.</returns>
    public static SetSecretManagerCommand WithExpiration(string? container, string secretKey, string secretValue, DateTimeOffset expirationDate, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(ExpirationDate)] = expirationDate
        };

        return new SetSecretManagerCommand(container, secretKey, secretValue, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a SetSecretManagerCommand with tags.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="secretValue">The secret value to store.</param>
    /// <param name="tags">The tags for the secret.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new SetSecretManagerCommand instance.</returns>
    public static SetSecretManagerCommand WithTags(string? container, string secretKey, string secretValue, IReadOnlyDictionary<string, string> tags, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(Tags)] = tags
        };

        return new SetSecretManagerCommand(container, secretKey, secretValue, parameters, null, timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new SetSecretManagerCommand(Container, SecretKey!, SecretValue, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new SetSecretManagerCommand(Container, SecretKey!, SecretValue, Parameters, newMetadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<SecretValue> ISecretManagerCommand<SecretValue>.WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new SetSecretManagerCommand(Container, SecretKey!, SecretValue, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<SecretValue> ISecretManagerCommand<SecretValue>.WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new SetSecretManagerCommand(Container, SecretKey!, SecretValue, Parameters, newMetadata, Timeout);
    }

    private static Dictionary<string, object?> CreateParametersWithValue(string secretValue, IReadOnlyDictionary<string, object?>? additionalParameters)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(SecretValue)] = secretValue
        };

        if (additionalParameters?.Count > 0)
        {
            foreach (var parameter in additionalParameters)
            {
                parameters[parameter.Key] = parameter.Value;
            }
        }

        return parameters;
    }
}