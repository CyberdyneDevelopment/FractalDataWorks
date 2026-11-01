using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers.Logging;

/// <summary>
/// MessageLogging for token revocation.
/// </summary>
/// <remarks>
/// EventId range: 91195–91198, 91205–91208. Only the <c>jti</c> and user id appear — never the token itself, which
/// is a bearer credential and reaches whoever can read logs.
/// </remarks>
[MessageLoggingTypeCode("TOKENMANAGER")]
internal static partial class RevocationLog
{
    /// <summary>A presented token is being checked against the deny-list.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The token being checked.</param>
    [MessageLogging(EventId = 91205, Level = LogLevel.Trace,
        Message = "Checking revocation for token {jti}")]
    internal static partial IGenericMessage Checking(ILogger logger, Guid jti);

    /// <summary>A token was deny-listed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The revoked token's <c>jti</c> claim.</param>
    [MessageLogging(EventId = 91195, Level = LogLevel.Information,
        Message = "Token {jti} revoked")]
    internal static partial IGenericMessage Revoked(ILogger logger, Guid jti);

    /// <summary>Writing the revocation row failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The token that could not be revoked.</param>
    [MessageLogging(EventId = 91196, Level = LogLevel.Error,
        Message = "Failed to revoke token {jti}")]
    internal static partial IGenericMessage RevokeFailed(ILogger logger, Guid jti);

    /// <summary>A presented token's <c>jti</c> was found on the deny-list.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The revoked token's <c>jti</c> claim.</param>
    [MessageLogging(EventId = 91197, Level = LogLevel.Warning,
        Message = "Rejected token {jti}: previously revoked")]
    internal static partial IGenericMessage PresentedRevoked(ILogger logger, Guid jti);

    /// <summary>Checking the deny-list failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The token whose revocation status could not be checked.</param>
    [MessageLogging(EventId = 91198, Level = LogLevel.Error,
        Message = "Failed to check revocation for token {jti}")]
    internal static partial IGenericMessage CheckFailed(ILogger logger, Guid jti);

    /// <summary>Every live session of a user was ended.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user.</param>
    /// <param name="revokedAt">The cut-off; tokens issued at or before it are rejected.</param>
    [MessageLogging(EventId = 91206, Level = LogLevel.Information,
        Message = "Ended every session of user {userId} issued at or before {revokedAt}")]
    internal static partial IGenericMessage UserRevoked(ILogger logger, Guid userId, DateTimeOffset revokedAt);

    /// <summary>Writing a user's session cut-off failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The user whose sessions could not be ended.</param>
    [MessageLogging(EventId = 91207, Level = LogLevel.Error,
        Message = "Failed to end the sessions of user {userId}")]
    internal static partial IGenericMessage UserRevokeFailed(ILogger logger, Guid userId);

    /// <summary>A presented token was issued before its subject's sessions were ended.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The rejected token's <c>jti</c> claim.</param>
    /// <param name="userId">The token's subject.</param>
    [MessageLogging(EventId = 91208, Level = LogLevel.Warning,
        Message = "Rejected token {jti}: issued before the sessions of user {userId} were ended")]
    internal static partial IGenericMessage PresentedUserRevoked(ILogger logger, Guid jti, Guid userId);
}
