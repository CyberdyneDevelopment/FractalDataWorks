using System;
using System.Collections.Generic;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Abstractions.Results;
using Fdw.Services.SecretManagers.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Commands;

/// <summary>
/// Command for verifying a credential (password or API key) against the secret store.
/// </summary>
public sealed class VerifyCredentialCommand : SecretManagerCommandBase, ISecretManagerCommand<CredentialVerificationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyCredentialCommand"/> class.
    /// </summary>
    /// <param name="userId">The user identifier to verify credentials for.</param>
    /// <param name="credentialType">The type of credential (e.g., "Password", "ApiKey").</param>
    /// <param name="candidateValue">The candidate credential value to verify.</param>
    /// <param name="timeout">Command timeout.</param>
    public VerifyCredentialCommand(
        Guid userId,
        string credentialType,
        string candidateValue,
        TimeSpan? timeout = null)
        : base(
            "VerifyCredential",
            container: null,
            secretKey: null,
            typeof(CredentialVerificationResult),
            BuildParameters(userId, credentialType, candidateValue),
            metadata: null,
            timeout)
    {
        UserId = userId;
        if (credentialType is null)
        {
            // Why: reported as a defect (FDW rule) — a command should return IGenericResult, not
            // throw. Left in place per instructions (constructors cannot return IGenericResult).
            VerifyCredentialCommandLog.RequiredValueMissing(NullLogger<VerifyCredentialCommand>.Instance, nameof(credentialType));
            throw new ArgumentNullException(nameof(credentialType));
        }

        if (candidateValue is null)
        {
            // Why: same throw-instead-of-result defect as above — logged, not converted.
            VerifyCredentialCommandLog.RequiredValueMissing(NullLogger<VerifyCredentialCommand>.Instance, nameof(candidateValue));
            throw new ArgumentNullException(nameof(candidateValue));
        }

        CredentialType = credentialType;
        CandidateValue = candidateValue;

        VerifyCredentialCommandLog.Constructed(NullLogger<VerifyCredentialCommand>.Instance, userId, credentialType);
    }

    /// <summary>
    /// Gets the user identifier to verify credentials for.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the type of credential (e.g., "Password", "ApiKey").
    /// </summary>
    public string CredentialType { get; }

    /// <summary>
    /// Gets the candidate credential value to verify.
    /// </summary>
    public string CandidateValue { get; }

    /// <inheritdoc/>
    public override bool IsSecretModifying => false;

    /// <inheritdoc/>
    protected override bool RequiresSecretKey() => false;

    /// <inheritdoc/>
    protected override bool RequiresContainer() => false;

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithParameters(IReadOnlyDictionary<string, object?> newParameters)
    {
        return new VerifyCredentialCommand(UserId, CredentialType, CandidateValue, Timeout);
    }

    /// <inheritdoc/>
    protected override ISecretManagerCommand CreateCopyWithMetadata(IReadOnlyDictionary<string, object> newMetadata)
    {
        return new VerifyCredentialCommand(UserId, CredentialType, CandidateValue, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<CredentialVerificationResult> ISecretManagerCommand<CredentialVerificationResult>.WithParameters(
        IReadOnlyDictionary<string, object?> newParameters)
    {
        return new VerifyCredentialCommand(UserId, CredentialType, CandidateValue, Timeout);
    }

    /// <inheritdoc/>
    ISecretManagerCommand<CredentialVerificationResult> ISecretManagerCommand<CredentialVerificationResult>.WithMetadata(
        IReadOnlyDictionary<string, object> newMetadata)
    {
        return new VerifyCredentialCommand(UserId, CredentialType, CandidateValue, Timeout);
    }

    private static Dictionary<string, object?> BuildParameters(Guid userId, string credentialType, string candidateValue)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["UserId"] = userId,
            ["CredentialType"] = credentialType,
            ["CandidateValue"] = candidateValue
        };
    }
}
