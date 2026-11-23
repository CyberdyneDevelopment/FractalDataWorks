using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the answer policies (FDW-6142): the refusals of judging a call. A policy or call that is not there is a not-found (3), a row that states something invalid is a validation (2) and a configuration
/// row that cannot be read is a dependency that failed (7). EventId range: 31450, 31451, 21452, 71453 and 10454 (information).
/// </summary>
[MessageLoggingTypeCode("ANSWERPOLICY")]
public static partial class AnswerPolicyLog
{
    /// <summary>Logs a policy that does not exist.</summary>
    [MessageLogging(EventId = 31450, Level = LogLevel.Error,
        Message = "Answer policy '{policy}' is not a policy of the caller's tenant",
        ClientMessage = "That answer policy does not exist")]
    public static partial IGenericMessage PolicyNotFound(ILogger logger, string policy);

    /// <summary>Logs a call a policy does not list.</summary>
    [MessageLogging(EventId = 31451, Level = LogLevel.Error,
        Message = "Answer policy '{policy}' does not list call '{call}', so what it requires is not known and none is assumed",
        ClientMessage = "That answer policy does not list that call")]
    public static partial IGenericMessage CallNotListed(ILogger logger, string policy, string call);

    /// <summary>Logs a policy row that states something invalid.</summary>
    [MessageLogging(EventId = 21452, Level = LogLevel.Error,
        Message = "Answer policy '{policy}' is not valid: {reason}",
        ClientMessage = "That answer policy is not valid")]
    public static partial IGenericMessage PolicyInvalid(ILogger logger, string policy, string reason);

    /// <summary>Logs a policy, or a grant it is judged against, that could not be read.</summary>
    [MessageLogging(EventId = 71453, Level = LogLevel.Error,
        Message = "Answer policy '{policy}' could not be read: {reason}")]
    public static partial IGenericMessage NotRead(ILogger logger, string policy, string? reason);

    /// <summary>Logs a call that was judged.</summary>
    [MessageLogging(EventId = 10454, Level = LogLevel.Information,
        Message = "Call '{call}' under answer policy '{policy}' was judged {outcome}")]
    public static partial IGenericMessage Judged(ILogger logger, string policy, string call, string outcome);
}
