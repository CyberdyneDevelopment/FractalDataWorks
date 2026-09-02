using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers.Logging;

/// <summary>
/// MessageLogging for token revocation.
/// </summary>
/// <remarks>
/// EventId range: 91195–91198. Only the <c>jti</c> appears — never the token itself, which is a
/// bearer credential and reaches whoever can read logs.
/// </remarks>
[MessageLoggingTypeCode("TOKENMANAGER")]
internal static partial class RevocationLog
{
    /// <summary>A token was deny-listed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The revoked token's <c>jti</c> claim.</param>
    [MessageLogging(EventId = 91195, Level = LogLevel.Information,
        Message = "Token {jti} revoked")]
    internal static partial IGenericMessage Revoked(ILogger logger, Guid jti);

    /// <summary>Writing the revocation row failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The token that could not be revoked.</param>
    /// <param name="reason">Why the write failed.</param>
    [MessageLogging(EventId = 91196, Level = LogLevel.Error,
        Message = "Failed to revoke token {jti}: {reason}")]
    internal static partial IGenericMessage RevokeFailed(ILogger logger, Guid jti, string reason);

    /// <summary>A presented token's <c>jti</c> was found on the deny-list.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The revoked token's <c>jti</c> claim.</param>
    [MessageLogging(EventId = 91197, Level = LogLevel.Warning,
        Message = "Rejected token {jti}: previously revoked")]
    internal static partial IGenericMessage PresentedRevoked(ILogger logger, Guid jti);

    /// <summary>Checking the deny-list failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jti">The token whose revocation status could not be checked.</param>
    /// <param name="reason">Why the read failed.</param>
    [MessageLogging(EventId = 91198, Level = LogLevel.Error,
        Message = "Failed to check revocation for token {jti}: {reason}")]
    internal static partial IGenericMessage CheckFailed(ILogger logger, Guid jti, string reason);
}
