using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ManagementCommand for retrieving a secret value from a secret provider.
/// </summary>
/// <remarks>
/// GetSecretManagerCommand retrieves the current or a specific version of a secret
/// from the configured secret store. The managementCommand can specify version requirements
/// and metadata filters through its parameters.
/// </remarks>
public sealed class GetSecretManagerCommand : SecretManagerCommandBase, ISecretManagerCommand<SecretValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSecretManagerCommand"/> class.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="parameters">ManagementCommand parameters (e.g., Version, IncludeMetadata).</param>
    /// <param name="metadata">Additional managementCommand metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <exception cref="ArgumentException">Thrown when secretKey is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    public GetSecretManagerCommand(
        string? container,
        string secretKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null)
        : base("GetSecret", container, secretKey, typeof(SecretValue), parameters, metadata, timeout)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            // Why: reported as a defect (FDW rule) — a command should return IGenericResult, not
            // throw. Left in place per instructions (constructors cannot return IGenericResult).
            GetSecretManagerCommandLog.RequiredValueMissing(NullLogger<GetSecretManagerCommand>.Instance, nameof(secretKey));
            throw new ArgumentException("Secret key cannot be null or empty for GetSecret operation.", nameof(secretKey));
        }

        GetSecretManagerCommandLog.Constructed(NullLogger<GetSecretManagerCommand>.Instance, container, secretKey);
    }

    /// <inheritdoc/>
    public override bool IsSecretModifying => false;

    /// <summary>
    /// Gets the version of the secret to retrieve.
    /// </summary>
    /// <value>The version identifier, or null to get the latest version.</value>
    public string? Version => Parameters.TryGetValue(nameof(Version), out var version) ? version?.ToString() : null;

    /// <summary>
    /// Gets a value indicating whether to include secret metadata in the result.
    /// </summary>
    /// <value><c>true</c> to include metadata; otherwise, <c>false</c>.</value>
    public bool IncludeMetadata => Parameters.TryGetValue(nameof(IncludeMetadata), out var include) &&
                                   include is bool includeMetadata && includeMetadata;

    /// <summary>
    /// Creates a GetSecretManagerCommand for the latest version of a secret.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="includeMetadata">Whether to include secret metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new GetSecretManagerCommand instance.</returns>
    public static GetSecretManagerCommand Latest(string? container, string secretKey, bool includeMetadata = false, TimeSpan? timeout = null)
    {
        var parameters = includeMetadata
            ? new Dictionary<string, object?>(StringComparer.Ordinal) { [nameof(IncludeMetadata)] = true }
            : null;

        return new GetSecretManagerCommand(container, secretKey, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a GetSecretManagerCommand for a specific version of a secret.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="includeMetadata">Whether to include secret metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new GetSecretManagerCommand instance.</returns>
    /// <exception cref="ArgumentException">Thrown when version is null or empty.</exception>
    public static GetSecretManagerCommand ForVersion(string? container, string secretKey, string version, bool includeMetadata = false, TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            // Why: same throw-instead-of-result defect as the constructor guard above — logged,
            // not converted.
            GetSecretManagerCommandLog.RequiredValueMissing(NullLogger<GetSecretManagerCommand>.Instance, nameof(version));
            throw new ArgumentException("Version cannot be null or empty.", nameof(version));
        }

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(Version)] = version
        };

        if (includeMetadata)
        {
            parameters[nameof(IncludeMetadata)] = true;
        }

        return new GetSecretManagerCommand(container, secretKey, parameters, null, timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new GetSecretManagerCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new GetSecretManagerCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<SecretValue> ISecretManagerCommand<SecretValue>.WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new GetSecretManagerCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<SecretValue> ISecretManagerCommand<SecretValue>.WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new GetSecretManagerCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }
}