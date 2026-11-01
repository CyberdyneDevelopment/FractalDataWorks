using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the Notify stage (FDW-1120): a stage that cannot send, a message that names a variable no run has, a
/// notification that fired and one that failed. Every refusal is logged and returned in the failure result.
/// EventId range: 20401-20405.
/// </summary>
[MessageLoggingTypeCode("TRANSFORM")]
public static partial class NotifyLog
{
    /// <summary>Logs a Notify stage on a host that has no notification rules to send through.</summary>
    [MessageLogging(EventId = 20401, Level = LogLevel.Error,
        Message = "Pipeline '{pipelineName}' has Notify stage '{stageName}' but this host cannot send through notification rules")]
    public static partial IGenericMessage NotifyRuleUnavailable(ILogger logger, string pipelineName, string stageName);

    /// <summary>Logs a Notify stage whose message uses a variable that no run has.</summary>
    [MessageLogging(EventId = 20402, Level = LogLevel.Error,
        Message = "Notify stage '{stageName}' message uses unknown variables [{unknown}]; a run has [{reserved}]")]
    public static partial IGenericMessage NotifyTemplateUnknownVariable(ILogger logger, string stageName, string unknown, string reserved);

    /// <summary>Logs a Notify stage that fired.</summary>
    [MessageLogging(EventId = 20403, Level = LogLevel.Information,
        Message = "Notify stage '{stageName}' fired at {runPoint} through rule '{ruleName}'")]
    public static partial IGenericMessage NotifyFired(ILogger logger, string stageName, string runPoint, string ruleName);

    /// <summary>Logs a Notify stage that could not send.</summary>
    [MessageLogging(EventId = 20404, Level = LogLevel.Error,
        Message = "Notify stage '{stageName}' could not send at {runPoint} through rule '{ruleName}': {reason}")]
    public static partial IGenericMessage NotifyFailed(ILogger logger, string stageName, string runPoint, string ruleName, string? reason);

    /// <summary>Logs a Notify stage that is unusable: it states no rule, run point or message, or names a run point that does not exist.</summary>
    [MessageLogging(EventId = 20405, Level = LogLevel.Error,
        Message = "Notify stage '{stageName}' is not usable: {reason}")]
    public static partial IGenericMessage NotifyStageInvalid(ILogger logger, string stageName, string reason);
}
