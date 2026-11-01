using System;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Result of a credential verification operation.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class CredentialVerificationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the credential is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the user identifier associated with the credential.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the username associated with the credential.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the credential, if applicable.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
