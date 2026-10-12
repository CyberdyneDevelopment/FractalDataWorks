using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// ManagementCommand for deleting a secret from a secret provider.
/// </summary>
/// <remarks>
/// DeleteSecretManagerCommand removes a secret from the configured secret store.
/// The managementCommand can specify whether to perform a soft delete (if supported)
/// or a permanent deletion through its parameters.
/// </remarks>
public sealed class DeleteSecretManagerCommand : SecretManagerCommandBase, ISecretManagerCommand<IGenericResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSecretManagerCommand"/> class.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="parameters">ManagementCommand parameters (e.g., PermanentDelete, RecoveryWindow).</param>
    /// <param name="metadata">Additional managementCommand metadata.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <exception cref="ArgumentException">Thrown when secretKey is null or empty.</exception>
    public DeleteSecretManagerCommand(
        string? container,
        string secretKey,
        IReadOnlyDictionary<string, object?>? parameters = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        TimeSpan? timeout = null)
        : base("DeleteSecret", container, secretKey, typeof(IGenericResult), parameters, metadata, timeout)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            DeleteSecretManagerCommandLog.RequiredValueMissing(NullLogger<DeleteSecretManagerCommand>.Instance, nameof(secretKey));
            throw new ArgumentException("Secret key cannot be null or empty for DeleteSecret operation.", nameof(secretKey));
        }

        DeleteSecretManagerCommandLog.Constructed(NullLogger<DeleteSecretManagerCommand>.Instance, container, secretKey);
    }

    /// <inheritdoc/>
    public override bool IsSecretModifying => true;

    /// <summary>
    /// Gets a value indicating whether to perform a permanent deletion.
    /// </summary>
    /// <value><c>true</c> for permanent deletion; <c>false</c> for soft deletion (if supported).</value>
    /// <remarks>
    /// Soft deletion allows secret recovery within a specified time window.
    /// Permanent deletion immediately removes the secret with no recovery option.
    /// </remarks>
    public bool PermanentDelete => Parameters.TryGetValue(nameof(PermanentDelete), out var permanent) &&
                                   permanent is bool isPermanent && isPermanent;

    /// <summary>
    /// Gets the recovery window for soft-deleted secrets.
    /// </summary>
    /// <value>The time period during which a soft-deleted secret can be recovered, or null for default.</value>
    public TimeSpan? RecoveryWindow => Parameters.TryGetValue(nameof(RecoveryWindow), out var window) &&
                                       window is TimeSpan recoveryWindow ? recoveryWindow : null;

    /// <summary>
    /// Creates a DeleteSecretManagerCommand for soft deletion.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new DeleteSecretManagerCommand instance for soft deletion.</returns>
    public static DeleteSecretManagerCommand SoftDelete(string? container, string secretKey, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(PermanentDelete)] = false
        };

        return new DeleteSecretManagerCommand(container, secretKey, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a DeleteSecretManagerCommand for permanent deletion.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new DeleteSecretManagerCommand instance for permanent deletion.</returns>
    public static DeleteSecretManagerCommand PermanentlyDelete(string? container, string secretKey, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(PermanentDelete)] = true
        };

        return new DeleteSecretManagerCommand(container, secretKey, parameters, null, timeout);
    }

    /// <summary>
    /// Creates a DeleteSecretManagerCommand for soft deletion with custom recovery window.
    /// </summary>
    /// <param name="container">The secret container or vault name.</param>
    /// <param name="secretKey">The secret key or identifier.</param>
    /// <param name="recoveryWindow">The time period during which the secret can be recovered.</param>
    /// <param name="timeout">ManagementCommand timeout.</param>
    /// <returns>A new DeleteSecretManagerCommand instance for soft deletion with specified recovery window.</returns>
    public static DeleteSecretManagerCommand SoftDeleteWithRecovery(string? container, string secretKey, TimeSpan recoveryWindow, TimeSpan? timeout = null)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(PermanentDelete)] = false,
            [nameof(RecoveryWindow)] = recoveryWindow
        };

        return new DeleteSecretManagerCommand(container, secretKey, parameters, null, timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new DeleteSecretManagerCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new DeleteSecretManagerCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<IGenericResult> ISecretManagerCommand<IGenericResult>.WithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new DeleteSecretManagerCommand(Container, SecretKey!, newParameters, Metadata, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<IGenericResult> ISecretManagerCommand<IGenericResult>.WithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new DeleteSecretManagerCommand(Container, SecretKey!, Parameters, newMetadata, Timeout);
    }
}