using System;

namespace Fdw.Services.SecretManagers.Abstractions.Results;

/// <summary>
/// Result of a credential storage operation.
/// </summary>
// Why: pure result/warning POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class CredentialStorageResult
{
    /// <summary>
    /// Gets or sets the identifier of the stored credential.
    /// </summary>
    public Guid CredentialId { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the credential, if applicable.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the raw value of the credential.
    /// Only populated for ApiKey type. Show once to user, never stored.
    /// </summary>
    public string? RawValue { get; set; }
}
