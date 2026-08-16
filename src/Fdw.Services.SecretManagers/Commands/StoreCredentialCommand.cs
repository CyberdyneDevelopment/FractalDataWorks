using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// Command for storing a credential (password hash or API key) in the secret store.
/// </summary>
public sealed class StoreCredentialCommand : SecretManagerCommandBase, ISecretManagerCommand<CredentialStorageResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreCredentialCommand"/> class.
    /// </summary>
    /// <param name="userId">The user identifier to store credentials for.</param>
    /// <param name="credentialType">The type of credential (e.g., "Password", "ApiKey").</param>
    /// <param name="plaintextValue">The plaintext credential value to hash and store.</param>
    /// <param name="expiresAt">Optional expiration time for the credential.</param>
    /// <param name="label">Optional label for the credential (e.g., API key description).</param>
    /// <param name="timeout">Command timeout.</param>
    public StoreCredentialCommand(
        Guid userId,
        string credentialType,
        string plaintextValue,
        DateTimeOffset? expiresAt = null,
        string? label = null,
        TimeSpan? timeout = null)
        : base(
            "StoreCredential",
            container: null,
            secretKey: null,
            typeof(CredentialStorageResult),
            BuildParameters(userId, credentialType, plaintextValue, expiresAt, label),
            metadata: null,
            timeout)
    {
        UserId = userId;
        if (credentialType is null)
        {
            // Why: reported as a defect (FDW rule) — a command should return IGenericResult, not
            // throw. Left in place per instructions (constructors cannot return IGenericResult).
            StoreCredentialCommandLog.RequiredValueMissing(NullLogger<StoreCredentialCommand>.Instance, nameof(credentialType));
            throw new ArgumentNullException(nameof(credentialType));
        }

        if (plaintextValue is null)
        {
            // Why: same throw-instead-of-result defect as above — logged, not converted.
            StoreCredentialCommandLog.RequiredValueMissing(NullLogger<StoreCredentialCommand>.Instance, nameof(plaintextValue));
            throw new ArgumentNullException(nameof(plaintextValue));
        }

        CredentialType = credentialType;
        PlaintextValue = plaintextValue;
        ExpiresAt = expiresAt;
        Label = label;

        StoreCredentialCommandLog.Constructed(NullLogger<StoreCredentialCommand>.Instance, userId, credentialType);
    }

    /// <summary>
    /// Gets the user identifier to store credentials for.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the type of credential (e.g., "Password", "ApiKey").
    /// </summary>
    public string CredentialType { get; }

    /// <summary>
    /// Gets the plaintext credential value to hash and store.
    /// </summary>
    public string PlaintextValue { get; }

    /// <summary>
    /// Gets the optional expiration time for the credential.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Gets the optional label for the credential.
    /// </summary>
    public string? Label { get; }

    /// <inheritdoc/>
    public override bool IsSecretModifying => true;

    /// <inheritdoc/>
    protected override bool RequiresSecretKey() => false;

    /// <inheritdoc/>
    protected override bool RequiresContainer() => false;

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new StoreCredentialCommand(UserId, CredentialType, PlaintextValue, ExpiresAt, Label, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new StoreCredentialCommand(UserId, CredentialType, PlaintextValue, ExpiresAt, Label, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<CredentialStorageResult> ISecretManagerCommand<CredentialStorageResult>.WithParameters(
        IReadOnlyDictionary<string, object?> newParameters)
    {
        return new StoreCredentialCommand(UserId, CredentialType, PlaintextValue, ExpiresAt, Label, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<CredentialStorageResult> ISecretManagerCommand<CredentialStorageResult>.WithMetadata(
        IReadOnlyDictionary<string, object> newMetadata)
    {
        return new StoreCredentialCommand(UserId, CredentialType, PlaintextValue, ExpiresAt, Label, Timeout);
    }

    private static Dictionary<string, object?> BuildParameters(
        Guid userId, string credentialType, string plaintextValue,
        DateTimeOffset? expiresAt, string? label)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["UserId"] = userId,
            ["CredentialType"] = credentialType,
            ["PlaintextValue"] = plaintextValue,
            ["ExpiresAt"] = expiresAt,
            ["Label"] = label
        };
    }
}
