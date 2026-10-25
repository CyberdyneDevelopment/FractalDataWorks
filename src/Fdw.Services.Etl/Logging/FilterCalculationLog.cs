using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the calculations a query filter names (FDW-2333): finding the stored calculation, checking the values the
/// filter supplies for its inputs, and running it. Every refusal is logged and returned in the failure result, naming the
/// calculation.
/// EventIds are result-code numbers: 10001 informational, 20000 required value missing, 20001 invalid input, 30000 not found,
/// 40000 invalid state, and the package's own conditions in the 21000 band.
/// </summary>
[MessageLoggingTypeCode("FILTERCALC")]
public static partial class FilterCalculationLog
{
    /// <summary>Logs that a filter's calculation was evaluated.</summary>
    [MessageLogging(EventId = 10001, Level = LogLevel.Information,
        Message = "Evaluated calculation '{calculationName}' with {inputCount} input value(s)")]
    public static partial IGenericMessage Evaluated(ILogger logger, string calculationName, int inputCount);

    /// <summary>Logs that no calculation has the name a filter gave.</summary>
    [MessageLogging(EventId = 30000, Level = LogLevel.Error,
        Message = "Calculation '{calculationName}' was not found",
        ClientMessage = "Calculation '{calculationName}' was not found")]
    public static partial IGenericMessage CalculationNotFound(ILogger logger, string calculationName);

    /// <summary>Logs a calculation that is not a Built transformation with a single value.</summary>
    [MessageLogging(EventId = 40000, Level = LogLevel.Error,
        Message = "Calculation '{calculationName}' is a {implementation} that gives {outputKind}, not one value; a calculation used for its value is a Built transformation with one kept result",
        ClientMessage = "Calculation '{calculationName}' is a {implementation} that gives {outputKind}, not one value; a calculation used for its value is a Built transformation with one kept result")]
    public static partial IGenericMessage NotASingleValue(ILogger logger, string calculationName, string implementation, string outputKind);

    /// <summary>Logs a calculation that keeps no result, or more than one.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "Calculation '{calculationName}' keeps {keptCount} results; the value of a calculation is that of the one result it keeps",
        ClientMessage = "Calculation '{calculationName}' keeps {keptCount} results; the value of a calculation is that of the one result it keeps")]
    public static partial IGenericMessage NotOneKeptResult(ILogger logger, string calculationName, int keptCount);

    /// <summary>Logs an input the calculation declares that the filter supplies no value for.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "No value is supplied for input '{inputName}' of calculation '{calculationName}'",
        ClientMessage = "No value is supplied for input '{inputName}' of calculation '{calculationName}'")]
    public static partial IGenericMessage InputNotSupplied(ILogger logger, string calculationName, string inputName);

    /// <summary>Logs a value the filter supplies for an input the calculation does not declare.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Error,
        Message = "A value is supplied for '{inputName}', which calculation '{calculationName}' does not declare as an input",
        ClientMessage = "A value is supplied for '{inputName}', which calculation '{calculationName}' does not declare as an input")]
    public static partial IGenericMessage InputNotDeclared(ILogger logger, string calculationName, string inputName);

    /// <summary>Logs a value of the wrong type for an input.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Input '{inputName}' of calculation '{calculationName}' is a {declaredType}, and the value supplied is a {suppliedType}",
        ClientMessage = "Input '{inputName}' of calculation '{calculationName}' is a {declaredType}, and the value supplied is a {suppliedType}")]
    public static partial IGenericMessage InputWrongType(ILogger logger, string calculationName, string inputName, string declaredType, string suppliedType);

    /// <summary>Logs a calculation that does not compile.</summary>
    [MessageLogging(EventId = 21002, Level = LogLevel.Error,
        Message = "Calculation '{calculationName}' cannot be compiled; the failure result names why")]
    public static partial IGenericMessage CalculationRefused(ILogger logger, string calculationName);

    /// <summary>Logs a calculation that failed while it ran.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error,
        Message = "Calculation '{calculationName}' failed while it was evaluated; the failure result names why")]
    public static partial IGenericMessage CalculationFailed(ILogger logger, string calculationName);
}
