using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Validation;

/// <summary>
/// MessageLogging for a request or configuration that FluentValidation refused. Its number is in the Validation band (2xxxx), so the HTTP
/// answer to a refusal built from it is the Validation category's 400, with the client message as the reason (FDW-3539).
/// </summary>
[MessageLoggingTypeCode("VALIDATION")]
public static partial class ValidationLog
{
    /// <summary>Logged when a validator refused its input; every failed rule is named, so the person sees what to correct.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning,
        Message = "The input failed validation: {details}",
        ClientMessage = "The input failed validation: {details}")]
    public static partial IGenericMessage InputRejected(ILogger logger, string details);
}
