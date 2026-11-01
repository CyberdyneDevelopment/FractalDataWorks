using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Logging;

/// <summary>
/// MessageLogging for the lookup of a person's user name (FDW-2199).
/// </summary>
[MessageLoggingTypeCode("USERS")]
public static partial class UserNamesLog
{
    /// <summary>Logs that a person's user name is being read.</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The person.</param>
    /// <returns>The structured message for the event.</returns>
    // Why Trace: an "about to" record of a per-call read.
    [MessageLogging(EventId = 11100, Level = LogLevel.Trace, Message = "Reading the user name of '{userId}'")]
    public static partial IGenericMessage Reading(ILogger logger, Guid userId);

    /// <summary>The signed-in person has no account to take a name from. An authentication failure (category 5).</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The person.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 51100, Level = LogLevel.Warning, Message = "The signed-in person '{userId}' has no user account, so there is no name to record")]
    public static partial IGenericMessage AccountNotFound(ILogger logger, Guid userId);

    /// <summary>The person's account has no user name. An authentication failure (category 5).</summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="userId">The person.</param>
    /// <returns>The structured message for the event.</returns>
    [MessageLogging(EventId = 51101, Level = LogLevel.Warning, Message = "The user account of '{userId}' has no user name, so there is no name to record")]
    public static partial IGenericMessage NameBlank(ILogger logger, Guid userId);
}
