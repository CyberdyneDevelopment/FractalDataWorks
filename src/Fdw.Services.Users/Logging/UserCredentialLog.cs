using System;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// Structured logging for user credential operations.
/// EventId range: 7830-7849
/// </summary>
public static partial class UserCredentialLog
{
    /// <summary>Logs that no current credential was found for the given user and secret type.</summary>
    [LoggerMessage(EventId = 7830, Level = LogLevel.Warning,
        Message = "No current credential found for user {UserId} type {SecretType}")]
    public static partial void CredentialNotFound(ILogger logger, Guid userId, string secretType);

    /// <summary>Logs that credential verification failed for the given user and secret type.</summary>
    [LoggerMessage(EventId = 7831, Level = LogLevel.Warning,
        Message = "Credential verification failed for user {UserId} type {SecretType}")]
    public static partial void VerificationFailed(ILogger logger, Guid userId, string secretType);

    /// <summary>Logs that a credential was successfully stored for the given user.</summary>
    [LoggerMessage(EventId = 7832, Level = LogLevel.Information,
        Message = "Credential stored for user {UserId} type {SecretType} algorithm {AlgorithmName}")]
    public static partial void Stored(ILogger logger, Guid userId, string secretType, string algorithmName);

    /// <summary>Logs that storing a credential failed for the given user and secret type.</summary>
    [LoggerMessage(EventId = 7833, Level = LogLevel.Error,
        Message = "Failed to store credential for user {UserId} type {SecretType}")]
    public static partial void StoreFailed(ILogger logger, Guid userId, string secretType);

    /// <summary>Logs that a force-password-change flag was set for the given user.</summary>
    [LoggerMessage(EventId = 7834, Level = LogLevel.Information,
        Message = "Force password change set for user {UserId}")]
    public static partial void ForceChangeSet(ILogger logger, Guid userId);

    /// <summary>Logs that setting the force-password-change flag failed for the given user.</summary>
    [LoggerMessage(EventId = 7835, Level = LogLevel.Error,
        Message = "Failed to set force password change for user {UserId}")]
    public static partial void ForceChangeFailed(ILogger logger, Guid userId);
}
