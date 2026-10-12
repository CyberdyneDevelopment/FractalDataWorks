using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Services.Authentication.Execution;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for <see cref="DatabaseAuthorizationCodeStore"/>.
/// </summary>
/// <remarks>
/// EventId range: 91270-91279. Neither a code nor a token appears here at any level: either one in a
/// log is a credential in a log.
/// </remarks>
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class DatabaseAuthorizationCodeStoreLog
{
    /// <summary>A sign-in code was issued for a completed flow.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow that issued the token.</param>
    [MessageLogging(EventId = 91270, Level = LogLevel.Trace,
        Message = "Sign-in code issued for flow '{flowName}'")]
    internal static partial IGenericMessage Issued(
        ILogger<DatabaseAuthorizationCodeStore> logger, string flowName);

    /// <summary>A sign-in code was redeemed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow that issued the token.</param>
    [MessageLogging(EventId = 91271, Level = LogLevel.Trace,
        Message = "Sign-in code redeemed for flow '{flowName}'")]
    internal static partial IGenericMessage Redeemed(
        ILogger<DatabaseAuthorizationCodeStore> logger, string flowName);

    /// <summary>The code presented is missing, expired, or already redeemed.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91272, Level = LogLevel.Warning,
        Message = "No sign-in code can be redeemed with the code presented")]
    internal static partial IGenericMessage NotRedeemable(ILogger<DatabaseAuthorizationCodeStore> logger);

    /// <summary>No code was supplied to redeem.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91273, Level = LogLevel.Error,
        Message = "A sign-in code must be supplied to redeem one")]
    internal static partial IGenericMessage CodeMissing(ILogger<DatabaseAuthorizationCodeStore> logger);

    /// <summary>A flow name or token was not supplied to issue a code.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91274, Level = LogLevel.Error,
        Message = "A flow name and an issued token must be supplied to issue a sign-in code")]
    internal static partial IGenericMessage IssueArgumentsMissing(ILogger<DatabaseAuthorizationCodeStore> logger);

    /// <summary>Writing the code row failed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow that issued the token.</param>
    [MessageLogging(EventId = 91275, Level = LogLevel.Error,
        Message = "The sign-in code for flow '{flowName}' could not be persisted to AuthDb")]
    internal static partial IGenericMessage IssueWriteFailed(
        ILogger<DatabaseAuthorizationCodeStore> logger, string flowName);

    /// <summary>A code was redeemed but its row could not be read back.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 91276, Level = LogLevel.Error,
        Message = "A redeemed sign-in code's row could not be read back from AuthDb")]
    internal static partial IGenericMessage RedeemReadBackFailed(ILogger<DatabaseAuthorizationCodeStore> logger);

    /// <summary>A redeemed code's token decrypted but could not be read as a token.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow that issued the token.</param>
    [MessageLogging(EventId = 91277, Level = LogLevel.Error,
        Message = "The token stored for flow '{flowName}' could not be read after decryption")]
    internal static partial IGenericMessage TokenUnreadable(
        ILogger<DatabaseAuthorizationCodeStore> logger, string flowName);

    /// <summary>
    /// The flow names no execution key, so what it stores cannot be encrypted or read. The provider
    /// only loads a keyless flow that stores nothing; reaching this means a step suspended, or a code
    /// was asked for, in a flow configured as never doing either (FDW-897).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 61010, Level = LogLevel.Error,
        Message = "Flow '{flowName}' names no execution key (SecretManagerName/SecretKeyName), so a sign-in code cannot be issued or redeemed for it")]
    internal static partial IGenericMessage NoExecutionKey(
        ILogger<DatabaseAuthorizationCodeStore> logger, string flowName);

    /// <summary>
    /// The flow sets no code lifetime, so a code cannot be issued for it. The provider only loads a
    /// flow without one when it has no ReturnUrls; reaching this means a code was asked for from a
    /// flow configured as never returning a browser (FDW-898).
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="flowName">The flow.</param>
    [MessageLogging(EventId = 61011, Level = LogLevel.Error,
        Message = "Flow '{flowName}' sets no CodeLifetime, so a sign-in code cannot be issued for it")]
    internal static partial IGenericMessage NoCodeLifetime(
        ILogger<DatabaseAuthorizationCodeStore> logger, string flowName);
}
