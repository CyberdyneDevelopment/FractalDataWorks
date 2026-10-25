using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for the log of sign-ins (FDW-2729). A user id and a flow name are safe to log; nothing the person presented is.
/// </summary>
[MessageLoggingTypeCode("SIGNIN")]
public static partial class SignInRecordLog
{
    /// <summary>A sign-in was recorded.</summary>
    [MessageLogging(EventId = 11520, Level = LogLevel.Debug,
        Message = "Recorded the sign-in of user '{userId}' by flow '{flowName}' at {at} (address known: {addressKnown}, user agent known: {agentKnown})")]
    public static partial IGenericMessage Recorded(ILogger logger, string userId, string flowName, string at, bool addressKnown, bool agentKnown);

    /// <summary>A sign-in could not be written.</summary>
    [MessageLogging(EventId = 91520, Level = LogLevel.Error,
        Message = "The sign-in of user '{userId}' was not recorded: {reason}")]
    public static partial IGenericMessage Failed(ILogger logger, string userId, string? reason);

    /// <summary>The last sign-ins could not be read.</summary>
    [MessageLogging(EventId = 91521, Level = LogLevel.Error,
        Message = "The last sign-in of {users} user(s) could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, int users, string? reason);

    /// <summary>One person's sign-ins could not be read.</summary>
    [MessageLogging(EventId = 91522, Level = LogLevel.Error,
        Message = "The sign-ins of user '{userId}' could not be read: {reason}")]
    public static partial IGenericMessage ListFailed(ILogger logger, string userId, string? reason);
}
