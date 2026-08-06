using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// Command for revoking a credential (e.g., an API key) in the secret store.
/// </summary>
public sealed class RevokeCredentialCommand : SecretManagerCommandBase, ISecretManagerCommand<bool>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevokeCredentialCommand"/> class.
    /// </summary>
    /// <param name="credentialId">The identifier of the credential to revoke.</param>
    /// <param name="credentialType">The type of credential (e.g., "ApiKey").</param>
    /// <param name="timeout">Command timeout.</param>
    public RevokeCredentialCommand(
        Guid credentialId,
        string credentialType,
        TimeSpan? timeout = null)
        : base(
            "RevokeCredential",
            container: null,
            secretKey: null,
            typeof(bool),
            BuildParameters(credentialId, credentialType),
            metadata: null,
            timeout)
    {
        CredentialId = credentialId;
        CredentialType = credentialType ?? throw new ArgumentNullException(nameof(credentialType));
    }

    /// <summary>
    /// Gets the identifier of the credential to revoke.
    /// </summary>
    public Guid CredentialId { get; }

    /// <summary>
    /// Gets the type of credential (e.g., "ApiKey").
    /// </summary>
    public string CredentialType { get; }

    /// <inheritdoc/>
    public override bool IsSecretModifying => true;

    /// <inheritdoc/>
    protected override bool RequiresSecretKey() => false;

    /// <inheritdoc/>
    protected override bool RequiresContainer() => false;

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new RevokeCredentialCommand(CredentialId, CredentialType, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new RevokeCredentialCommand(CredentialId, CredentialType, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<bool> ISecretManagerCommand<bool>.WithParameters(
        IReadOnlyDictionary<string, object?> newParameters)
    {
        return new RevokeCredentialCommand(CredentialId, CredentialType, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<bool> ISecretManagerCommand<bool>.WithMetadata(
        IReadOnlyDictionary<string, object> newMetadata)
    {
        return new RevokeCredentialCommand(CredentialId, CredentialType, Timeout);
    }

    private static Dictionary<string, object?> BuildParameters(Guid credentialId, string credentialType)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["CredentialId"] = credentialId,
            ["CredentialType"] = credentialType
        };
    }
}
