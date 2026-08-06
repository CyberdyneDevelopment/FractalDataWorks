using Fdw.Services.SecretManagers.Abstractions;
using System;
using System.Collections.Generic;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ManagementCommand for retrieving all versions of a specific secret from a secret provider.
/// </summary>
/// <remarks>
/// GetSecretManagerVersionsCommand retrieves version information for all versions of a secret
/// from the configured secret store. This includes version identifiers, creation dates,
/// and other version-specific metadata.
/// </remarks>
public sealed class GetSecretManagerVersionsCommand : SecretManagerCommandBase, ISecretManagerCommand<IEnumerable<SecretValue>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSecretManagerVersionsCommand"/> class.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="parameters">ManagementCommand parameters (e.g., IncludeDisabled, MaxResults).</param>
    /// <param name="metadata">Additional managementCommand metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <exception cref="ArgumentException">Thrown when secretKey is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public GetSecretManagerVersionsCommand(
        string? container,
        string secretKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null)
        : base("GetSecretVersions", container, secretKey, typeof(IEnumerable<SecretValue>), parameters, metadata, timeout)
    {
    }

    /// <summary>
    /// Creates a GetSecretManagerVersionsCommand for retrieving all versions of a secret.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key to retrieve versions for.</param>
    /// <param name="includeDisabled">Whether to include disabled/deleted versions.</param>
    /// <param name="maxResults">Maximum number of versions to retrieve.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new GetSecretManagerVersionsCommand instance.</returns>
    public static GetSecretManagerVersionsCommand Create(
        string? container,
        string secretKey,
        bool includeDisabled = false,
        int? maxResults = null,
        TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (includeDisabled)
            parameters["IncludeDisabled"] = true;

        if (maxResults.HasValue)
            parameters["MaxResults"] = maxResults.Value;

        return new GetSecretManagerVersionsCommand(container, secretKey, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a new managementCommand with updated parameters while preserving other properties.
    /// </summary>
    /// <param name="parameters">The new parameters dictionary.</param>
    /// <returns>A new GetSecretManagerVersionsCommand instance with updated parameters.</returns>
    public new GetSecretManagerVersionsCommand WithParameters(IReadOnlyDictionary<string, object?> parameters)
    {
        var newParameters = new Dictionary<string, object?>(Parameters, StringComparer.Ordinal);
        foreach (var kvp in parameters)
        {
            newParameters[kvp.Key] = kvp.Value;
        }

        return new GetSecretManagerVersionsCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <summary>
    /// Creates a new managementCommand with updated metadata while preserving other properties.
    /// </summary>
    /// <param name="metadata">The new metadata dictionary.</param>
    /// <returns>A new GetSecretManagerVersionsCommand instance with updated metadata.</returns>
    public new GetSecretManagerVersionsCommand WithMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        var newMetadata = new Dictionary<string, object>(Metadata, StringComparer.Ordinal);
        foreach (var kvp in metadata)
        {
            newMetadata[kvp.Key] = kvp.Value;
        }

        return new GetSecretManagerVersionsCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }

    /// <summary>
    /// Creates a new managementCommand with updated timeout while preserving other properties.
    /// </summary>
    /// <param name="timeout">The new timeout value.</param>
    /// <returns>A new GetSecretManagerVersionsCommand instance with updated timeout.</returns>
    public GetSecretManagerVersionsCommand WithTimeout(TimeSpan timeout)
    {
        return new GetSecretManagerVersionsCommand(Container, SecretKey!, Parameters, Metadata, timeout);
    }

    /// <inheritdoc/>
    public override bool IsSecretModifying => false;

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return WithParameters(newParameters);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return WithMetadata(newMetadata);
    }

    ISecretManagerCommand<IEnumerable<SecretValue>> ISecretManagerCommand<IEnumerable<SecretValue>>.WithParameters(IReadOnlyDictionary<string, object?> parameters)
    {
        return WithParameters(parameters);
    }

    ISecretManagerCommand<IEnumerable<SecretValue>> ISecretManagerCommand<IEnumerable<SecretValue>>.WithMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        return WithMetadata(metadata);
    }
}