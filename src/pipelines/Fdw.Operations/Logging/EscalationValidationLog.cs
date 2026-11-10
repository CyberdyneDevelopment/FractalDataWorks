using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Logging;

/// <summary>
/// MessageLogging for the refusals of an escalation level's severity and a level recipient's type (FDW-4693). EventIds 21090-21093: one for each reason, so a
/// caller can tell a value that was left out from one the collections do not hold.
/// </summary>
[MessageLoggingTypeCode("OPERATIONS")]
public static partial class EscalationValidationLog
{
    /// <summary>Logs an escalation level written without a severity.</summary>
    [MessageLogging(EventId = 21090, Level = LogLevel.Warning, Message = "An escalation level was written without a severity", ClientMessage = "Severity is required: state one of the escalation severities")]
    public static partial IGenericMessage SeverityNotStated(ILogger logger);

    /// <summary>Logs an escalation level written with a severity the collection does not hold.</summary>
    [MessageLogging(EventId = 21091, Level = LogLevel.Warning, Message = "An escalation level was written with the severity '{severity}', which is not an escalation severity (choices: {choices})", ClientMessage = "Severity '{severity}' is not an escalation severity. Choose one of: {choices}")]
    public static partial IGenericMessage SeverityUnknown(ILogger logger, string severity, string choices);

    /// <summary>Logs an escalation level recipient written without a recipient type.</summary>
    [MessageLogging(EventId = 21092, Level = LogLevel.Warning, Message = "An escalation level recipient was written without a recipient type", ClientMessage = "RecipientType is required: state one of the escalation recipient types")]
    public static partial IGenericMessage RecipientTypeNotStated(ILogger logger);

    /// <summary>Logs an escalation level recipient written with a recipient type the collection does not hold.</summary>
    [MessageLogging(EventId = 21093, Level = LogLevel.Warning, Message = "An escalation level recipient was written with the recipient type '{recipientType}', which is not an escalation recipient type (choices: {choices})", ClientMessage = "RecipientType '{recipientType}' is not an escalation recipient type. Choose one of: {choices}")]
    public static partial IGenericMessage RecipientTypeUnknown(ILogger logger, string recipientType, string choices);
}
