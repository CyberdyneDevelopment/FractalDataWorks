using Fdw.Services.SecretManagers.Abstractions;
using System;
using System.Collections.Generic;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ManagementCommand for listing secrets in a secret provider container.
/// </summary>
/// <remarks>
/// ListSecretsManagerCommand retrieves a list of secrets available in the configured secret store.
/// The managementCommand can specify filtering criteria, pagination, and metadata inclusion
/// through its parameters.
/// </remarks>
public sealed class ListSecretsManagerCommand : SecretManagerCommandBase, ISecretManagerCommand<IReadOnlyList<ISecretMetadata>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSecretsManagerCommand"/> class.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="parameters">ManagementCommand parameters (e.g., Filter, MaxResults, IncludeDeleted).</param>
    /// <param name="metadata">Additional managementCommand metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    public ListSecretsManagerCommand(
        string? container,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null)
        : base("ListSecrets", container, null, typeof(IReadOnlyList<ISecretMetadata>), parameters, metadata, timeout)
    {
    }

    /// <inheritdoc/>
    public override bool IsSecretModifying => false;

    /// <summary>
    /// Gets the filter pattern for secret names.
    /// </summary>
    /// <value>A pattern to filter secret names, or null for no filtering.</value>
    /// <remarks>
    /// The filter pattern syntax depends on the secret provider.
    /// Common patterns include wildcards (*), regular expressions, or prefix matching.
    /// </remarks>
    public string? Filter => Parameters.TryGetValue(nameof(Filter), out var filter) ? filter?.ToString() : null;

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    /// <value>The maximum number of secrets to return, or null for no limit.</value>
    public int? MaxResults => Parameters.TryGetValue(nameof(MaxResults), out var max) &&
                              max is int maxResults ? maxResults : null;

    /// <summary>
    /// Gets a value indicating whether to include deleted secrets in the results.
    /// </summary>
    /// <value><c>true</c> to include deleted secrets; otherwise, <c>false</c>.</value>
    public bool IncludeDeleted => Parameters.TryGetValue(nameof(IncludeDeleted), out var include) &&
                                  include is bool includeDeleted && includeDeleted;

    /// <summary>
    /// Gets the continuation token for paginated results.
    /// </summary>
    /// <value>A continuation token from a previous request, or null for the first page.</value>
    public string? ContinuationToken => Parameters.TryGetValue(nameof(ContinuationToken), out var token) ? token?.ToString() : null;

    /// <summary>
    /// Gets a value indicating whether to include secret versions in the metadata.
    /// </summary>
    /// <value><c>true</c> to include version information; otherwise, <c>false</c>.</value>
    public bool IncludeVersions => Parameters.TryGetValue(nameof(IncludeVersions), out var include) &&
                                   include is bool includeVersions && includeVersions;

    /// <summary>
    /// Creates a ListSecretsManagerCommand for all secrets in a container.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new ListSecretsManagerCommand instance.</returns>
    public static ListSecretsManagerCommand All(string? container, TimeSpan? timeout = null)
    {
        return new ListSecretsManagerCommand(container, null, null, timeout);
    }

    /// <summary>
    /// Creates a ListSecretsManagerCommand with a name filter.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="filter">The filter pattern for secret names.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new ListSecretsManagerCommand instance with filtering.</returns>
    public static ListSecretsManagerCommand WithFilter(string? container, string filter, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(Filter)] = filter
        };

        return new ListSecretsManagerCommand(container, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a ListSecretsManagerCommand with pagination.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="maxResults">The maximum number of results to return.</param>
    /// <param name="continuationToken">The continuation token from a previous request.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new ListSecretsManagerCommand instance with pagination.</returns>
    public static ListSecretsManagerCommand WithPagination(string? container, int maxResults, string? continuationToken = null, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(MaxResults)] = maxResults
        };

        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            parameters[nameof(ContinuationToken)] = continuationToken;
        }

        return new ListSecretsManagerCommand(container, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a ListSecretsManagerCommand that includes deleted secrets.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="includeVersions">Whether to include version information.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new ListSecretsManagerCommand instance that includes deleted secrets.</returns>
    public static ListSecretsManagerCommand IncludingDeleted(string? container, bool includeVersions = false, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(IncludeDeleted)] = true
        };

        if (includeVersions)
        {
            parameters[nameof(IncludeVersions)] = true;
        }

        return new ListSecretsManagerCommand(container, parameters, null, timeout);
    }

    /// <inheritdoc/>
    protected override bool RequiresSecretKey()
    {
        return false; // ListSecrets doesn't require a specific secret key
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new ListSecretsManagerCommand(Container, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new ListSecretsManagerCommand(Container, Parameters, newMetadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<IReadOnlyList<ISecretMetadata>> ISecretManagerCommand<IReadOnlyList<ISecretMetadata>>.WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new ListSecretsManagerCommand(Container, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<IReadOnlyList<ISecretMetadata>> ISecretManagerCommand<IReadOnlyList<ISecretMetadata>>.WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new ListSecretsManagerCommand(Container, Parameters, newMetadata, Timeout);
    }
}