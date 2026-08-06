using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for calculation operation execution.
/// EventId range: 5300-5330
/// </summary>
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationOperationLog
{
    // --- Operation execution lifecycle ---

    /// <summary>Traces that a calculation operation is starting execution.</summary>
    [MessageLogging(EventId = 11049, Level = LogLevel.Trace, Message = "Executing operation '{operationName}' in category '{category}'")]
    public static partial IGenericMessage OperationExecutionStarted(ILogger logger, string operationName, string category);

    /// <summary>Traces that a calculation operation completed successfully.</summary>
    [MessageLogging(EventId = 11050, Level = LogLevel.Trace, Message = "Operation '{operationName}' completed successfully")]
    public static partial IGenericMessage OperationExecutionSucceeded(ILogger logger, string operationName);

    /// <summary>Logs that a calculation operation failed with an exception.</summary>
    [MessageLogging(EventId = 91006, Level = LogLevel.Error, Message = "Operation '{operationName}' failed")]
    public static partial IGenericMessage OperationExecutionFailed(ILogger logger, Exception ex, string operationName);

    // --- Parameter validation ---

    /// <summary>Traces that parameter validation is starting for an operation.</summary>
    [MessageLogging(EventId = 11051, Level = LogLevel.Trace, Message = "Validating parameters for operation '{operationName}'")]
    public static partial IGenericMessage ParameterValidationStarted(ILogger logger, string operationName);

    /// <summary>Logs that a required parameter was not supplied.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error, Message = "Required parameter '{parameterName}' is missing for operation '{operationName}'")]
    public static partial IGenericMessage RequiredParameterMissing(ILogger logger, string parameterName, string operationName);

    /// <summary>Logs that a parameter value could not be converted to the expected type.</summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error, Message = "Parameter '{parameterName}' for operation '{operationName}' has invalid type: expected {expectedType}")]
    public static partial IGenericMessage ParameterTypeMismatch(ILogger logger, string parameterName, string operationName, string expectedType);

    /// <summary>Traces that parameter validation passed for an operation.</summary>
    [MessageLogging(EventId = 11052, Level = LogLevel.Trace, Message = "Parameter validation passed for operation '{operationName}'")]
    public static partial IGenericMessage ParameterValidationPassed(ILogger logger, string operationName);

    // --- Arithmetic operations ---

    /// <summary>Logs that a division by zero was attempted.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Warning, Message = "Division by zero in operation '{operationName}'")]
    public static partial IGenericMessage DivisionByZero(ILogger logger, string operationName);

    // --- Aggregate operations ---

    /// <summary>Traces that an aggregate operation is processing rows.</summary>
    [MessageLogging(EventId = 11053, Level = LogLevel.Trace, Message = "Aggregate operation '{operationName}' processing {rowCount} values")]
    public static partial IGenericMessage AggregateProcessing(ILogger logger, string operationName, int rowCount);

    /// <summary>Logs that an aggregate operation received an empty value set.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Debug, Message = "Aggregate operation '{operationName}' received empty value set")]
    public static partial IGenericMessage AggregateEmptyValues(ILogger logger, string operationName);

    // --- Window operations ---

    /// <summary>Traces that a window operation is starting with partition details.</summary>
    [MessageLogging(EventId = 11054, Level = LogLevel.Trace, Message = "Window operation '{operationName}' starting with {partitionFieldCount} partition fields and {orderFieldCount} order fields")]
    public static partial IGenericMessage WindowOperationStarted(ILogger logger, string operationName, int partitionFieldCount, int orderFieldCount);

    /// <summary>Traces that a window operation completed with result count.</summary>
    [MessageLogging(EventId = 11055, Level = LogLevel.Trace, Message = "Window operation '{operationName}' completed with {resultCount} results")]
    public static partial IGenericMessage WindowOperationCompleted(ILogger logger, string operationName, int resultCount);

    // --- Conditional operations ---

    /// <summary>Traces that a conditional operation is evaluating its condition.</summary>
    [MessageLogging(EventId = 11056, Level = LogLevel.Trace, Message = "Conditional operation '{operationName}' evaluating condition")]
    public static partial IGenericMessage ConditionalEvaluating(ILogger logger, string operationName);

    /// <summary>Traces the outcome of a conditional operation.</summary>
    [MessageLogging(EventId = 11057, Level = LogLevel.Trace, Message = "Conditional operation '{operationName}' resolved to '{branch}' branch")]
    public static partial IGenericMessage ConditionalResolved(ILogger logger, string operationName, string branch);

    // --- Coalesce operations ---

    /// <summary>Traces that a coalesce operation found a non-null value at a given position.</summary>
    [MessageLogging(EventId = 11058, Level = LogLevel.Trace, Message = "Coalesce operation found non-null value at position {position} of {totalFields}")]
    public static partial IGenericMessage CoalesceValueFound(ILogger logger, int position, int totalFields);

    /// <summary>Logs that a coalesce operation found all values to be null.</summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Warning, Message = "Coalesce operation '{operationName}' found all {totalFields} values to be null")]
    public static partial IGenericMessage CoalesceAllNull(ILogger logger, string operationName, int totalFields);

    // --- Step execution ---

    /// <summary>Traces that a calculation step is starting execution.</summary>
    [MessageLogging(EventId = 11059, Level = LogLevel.Trace, Message = "Executing calculation step '{stepName}' (ordinal {ordinal}) using operation '{operationName}'")]
    public static partial IGenericMessage StepExecutionStarted(ILogger logger, string stepName, int ordinal, string operationName);

    /// <summary>Traces that a calculation step completed and produced an output alias.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Trace, Message = "Calculation step '{stepName}' completed, output alias '{outputAlias}'")]
    public static partial IGenericMessage StepExecutionCompleted(ILogger logger, string stepName, string outputAlias);

    /// <summary>Logs that a calculation step failed.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error, Message = "Calculation step '{stepName}' failed")]
    public static partial IGenericMessage StepExecutionFailed(ILogger logger, Exception ex, string stepName);
}
