using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for calculation step execution.
/// Lifecycle EventIds 11064-11070; validation failures 21007-21015.
/// </summary>
/// <remarks>
/// Every entry below the lifecycle block is a hard stop. The step executor deliberately has no
/// "skip and continue" path — a calculation that cannot be evaluated exactly as configured must
/// fail rather than emit a number whose provenance cannot be explained.
/// </remarks>
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationStepExecutorLog
{
    // --- Lifecycle ---

    /// <summary>Traces that step execution is starting for a calculation.</summary>
    [MessageLogging(EventId = 11064, Level = LogLevel.Trace, Message = "Executing {stepCount} calculation step(s)")]
    public static partial IGenericMessage StepExecutionStarted(ILogger logger, int stepCount);

    /// <summary>Traces that an individual step is about to run.</summary>
    [MessageLogging(EventId = 11065, Level = LogLevel.Trace, Message = "Step {ordinal} '{stepName}' running operation '{operationType}'")]
    public static partial IGenericMessage StepStarted(ILogger logger, int ordinal, string stepName, string operationType);

    /// <summary>Traces that an individual step produced a value under its output alias.</summary>
    [MessageLogging(EventId = 11066, Level = LogLevel.Trace, Message = "Step '{stepName}' published result as '{outputAlias}'")]
    public static partial IGenericMessage StepCompleted(ILogger logger, string stepName, string outputAlias);

    /// <summary>Traces that all steps completed and a final value is being returned.</summary>
    [MessageLogging(EventId = 11067, Level = LogLevel.Trace, Message = "Step execution completed; final value from alias '{outputAlias}'")]
    public static partial IGenericMessage StepExecutionCompleted(ILogger logger, string outputAlias);

    // --- Hard stops ---

    /// <summary>Logs that a calculation declared no steps to execute.</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error, Message = "Calculation has no steps to execute")]
    public static partial IGenericMessage NoStepsConfigured(ILogger logger);

    /// <summary>Logs that a step element was not a calculation step configuration.</summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Error, Message = "Step element is '{actualType}', not a calculation step configuration")]
    public static partial IGenericMessage StepTypeUnexpected(ILogger logger, string actualType);

    /// <summary>Logs that a step named an operation that is not registered.</summary>
    [MessageLogging(EventId = 21009, Level = LogLevel.Error, Message = "Step '{stepName}' names operation '{operationType}', which is not a registered calculation operation")]
    public static partial IGenericMessage UnknownOperation(ILogger logger, string stepName, string operationType);

    /// <summary>Logs that an operand carried no name to bind to an operation parameter.</summary>
    [MessageLogging(EventId = 21010, Level = LogLevel.Error, Message = "Step '{stepName}' has an operand with no name; an operand name must match an operation parameter name")]
    public static partial IGenericMessage OperandNameMissing(ILogger logger, string stepName);

    /// <summary>Logs that an operand declared an unrecognised source type.</summary>
    [MessageLogging(EventId = 21011, Level = LogLevel.Error, Message = "Operand '{operandName}' on step '{stepName}' has operand type '{operandType}'; expected Input, StepReference, or Literal")]
    public static partial IGenericMessage UnknownOperandType(ILogger logger, string operandName, string stepName, string operandType);

    /// <summary>Logs that an operand referenced an input alias that was not resolved.</summary>
    [MessageLogging(EventId = 21012, Level = LogLevel.Error, Message = "Operand '{operandName}' on step '{stepName}' references input alias '{inputAlias}', which was not supplied")]
    public static partial IGenericMessage InputAliasUnresolved(ILogger logger, string operandName, string stepName, string inputAlias);

    /// <summary>Logs that an operand referenced a step alias that has not been produced.</summary>
    [MessageLogging(EventId = 21013, Level = LogLevel.Error, Message = "Operand '{operandName}' on step '{stepName}' references step alias '{stepAlias}', which no earlier step produced")]
    public static partial IGenericMessage StepAliasUnresolved(ILogger logger, string operandName, string stepName, string stepAlias);

    /// <summary>Logs that a literal operand carried no value.</summary>
    [MessageLogging(EventId = 21014, Level = LogLevel.Error, Message = "Operand '{operandName}' on step '{stepName}' is a literal but carries no value")]
    public static partial IGenericMessage LiteralValueMissing(ILogger logger, string operandName, string stepName);

    /// <summary>Logs that a required operation parameter had no operand bound to it.</summary>
    [MessageLogging(EventId = 21015, Level = LogLevel.Error, Message = "Operation '{operationType}' on step '{stepName}' requires parameter '{parameterName}', which no operand supplied")]
    public static partial IGenericMessage RequiredParameterMissing(ILogger logger, string operationType, string stepName, string parameterName);

    /// <summary>Logs that a step declared no output alias.</summary>
    [MessageLogging(EventId = 21016, Level = LogLevel.Error, Message = "Step '{stepName}' declares no output alias; later steps could not reference its result")]
    public static partial IGenericMessage OutputAliasMissing(ILogger logger, string stepName);

    /// <summary>Logs that two steps declared the same output alias.</summary>
    [MessageLogging(EventId = 21017, Level = LogLevel.Error, Message = "Step '{stepName}' reuses output alias '{outputAlias}', which an earlier step already published")]
    public static partial IGenericMessage DuplicateOutputAlias(ILogger logger, string stepName, string outputAlias);

    /// <summary>Logs that a field was requested from a value that carries no addressable fields.</summary>
    [MessageLogging(EventId = 21018, Level = LogLevel.Error, Message = "Operand '{operandName}' on step '{stepName}' requests field '{fieldName}', but the referenced value is '{actualType}', which exposes no such field")]
    public static partial IGenericMessage FieldNotAddressable(ILogger logger, string operandName, string stepName, string fieldName, string actualType);
}
