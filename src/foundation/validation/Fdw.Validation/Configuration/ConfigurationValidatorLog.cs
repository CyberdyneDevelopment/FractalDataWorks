using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Validation.Configuration;

/// <summary>
/// MessageLogging for finding the validator of a configuration class before it is written (FDW-3541). Both refusals are in the Configuration
/// band (6xxxx): a configuration class that states neither a validator nor a reason is a defect in the host's declarations, not in the request.
/// </summary>
[MessageLoggingTypeCode("VALIDATION")]
public static partial class ConfigurationValidatorLog
{
    /// <summary>Logged when no option speaks for a configuration class, its base classes or its interfaces.</summary>
    [MessageLogging(EventId = 60001, Level = LogLevel.Error,
        Message = "No validator and no NoValidation statement is declared for configuration class '{configurationType}', so it is not written; declare an option in ConfigurationValidators for it")]
    public static partial IGenericMessage NoValidatorDeclared(ILogger logger, string configurationType);

    /// <summary>Logged when more than one option speaks for the same configuration class at the same distance.</summary>
    [MessageLogging(EventId = 60002, Level = LogLevel.Error,
        Message = "More than one validator option speaks for configuration class '{configurationType}' ({options}), so it is not written; keep one")]
    public static partial IGenericMessage AmbiguousValidatorDeclared(ILogger logger, string configurationType, string options);

    /// <summary>Logged when a configuration was refused by the validator its class declares; every failed rule is named.</summary>
    [MessageLogging(EventId = 20003, Level = LogLevel.Warning,
        Message = "The '{configurationType}' configuration failed validation: {details}",
        ClientMessage = "The '{configurationType}' configuration failed validation: {details}")]
    public static partial IGenericMessage ConfigurationRejected(ILogger logger, string configurationType, string details);
}
