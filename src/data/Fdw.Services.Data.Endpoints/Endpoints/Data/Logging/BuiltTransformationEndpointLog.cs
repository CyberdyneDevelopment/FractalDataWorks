using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for the endpoints that create and replace a Built transformation (FDW-1219): what is refused about its
/// inputs and formulas before anything is stored.
/// </summary>
/// <remarks>
/// EventIds follow the result categories (first digit of a five-digit id): 1 progress, 2 validation (400). Validation uses
/// 21550-21559 and 21570-21579, and progress 11560-11561.
/// </remarks>
[MessageLoggingTypeCode("BUILTTRANSFORM")]
public static partial class BuiltTransformationEndpointLog
{
    /// <summary>Logs a Built transformation whose results cannot be typed, so a list shows them without a value type.</summary>
    [MessageLogging(EventId = 21576, Level = LogLevel.Warning,
        Message = "The results of Built transformation '{transformationName}' cannot be typed because the calculation engine refuses its definition; they are shown without a value type")]
    public static partial IGenericMessage ValueTypesUnknown(ILogger logger, string transformationName);

    /// <summary>Logs an input with a blank name.</summary>
    [MessageLogging(EventId = 21550, Level = LogLevel.Warning, Message = "A Built transformation input has a blank name")]
    public static partial IGenericMessage InputBlank(ILogger logger);

    /// <summary>Logs an input declared twice.</summary>
    [MessageLogging(EventId = 21551, Level = LogLevel.Warning, Message = "Input '{inputName}' is declared twice; an input has one declaration")]
    public static partial IGenericMessage InputRepeated(ILogger logger, string inputName);

    /// <summary>Logs an input whose value type is neither Number nor Flag.</summary>
    [MessageLogging(EventId = 21552, Level = LogLevel.Warning, Message = "Input '{inputName}' has value type '{valueType}'; it must be Number or Flag")]
    public static partial IGenericMessage InputTypeUnknown(ILogger logger, string inputName, string valueType);

    /// <summary>Logs a result that states no formula.</summary>
    [MessageLogging(EventId = 21553, Level = LogLevel.Warning, Message = "Result '{resultName}' states no formula")]
    public static partial IGenericMessage FormulaMissing(ILogger logger, string resultName);

    /// <summary>Logs a formula nested deeper than the limit.</summary>
    [MessageLogging(EventId = 21554, Level = LogLevel.Warning, Message = "The formula of result '{resultName}' is nested deeper than the limit of {maxDepth} levels")]
    public static partial IGenericMessage FormulaTooDeep(ILogger logger, string resultName, int maxDepth);

    /// <summary>Logs a piece of a formula that states no operand list.</summary>
    [MessageLogging(EventId = 21555, Level = LogLevel.Warning, Message = "A piece of the formula of result '{resultName}' states no operand list; state an empty list for a piece without operands")]
    public static partial IGenericMessage OperandsMissing(ILogger logger, string resultName);

    /// <summary>Logs a formula that reads a field no input declares.</summary>
    [MessageLogging(EventId = 21556, Level = LogLevel.Warning, Message = "The formula of result '{resultName}' reads field '{fieldName}', which is not a declared input")]
    public static partial IGenericMessage FieldNotDeclared(ILogger logger, string resultName, string fieldName);

    /// <summary>Logs a transformation that is not a Built one.</summary>
    [MessageLogging(EventId = 21557, Level = LogLevel.Warning, Message = "Transformation '{transformationName}' is a {implementation}, not a Built transformation")]
    public static partial IGenericMessage NotBuilt(ILogger logger, string transformationName, string implementation);

    /// <summary>Logs a table input that cannot be used.</summary>
    [MessageLogging(EventId = 21570, Level = LogLevel.Warning, Message = "Table input '{tableName}' is invalid: {reason}")]
    public static partial IGenericMessage TableInputInvalid(ILogger logger, string tableName, string reason);

    /// <summary>Logs a derived value that cannot be used.</summary>
    [MessageLogging(EventId = 21571, Level = LogLevel.Warning, Message = "Derived value '{valueName}' is invalid: {reason}")]
    public static partial IGenericMessage DerivedInvalid(ILogger logger, string valueName, string reason);

    /// <summary>Logs a table output that cannot be used.</summary>
    [MessageLogging(EventId = 21572, Level = LogLevel.Warning, Message = "Table output '{outputName}' is invalid: {reason}")]
    public static partial IGenericMessage OutputInvalid(ILogger logger, string outputName, string reason);

    /// <summary>Logs a formula or copy that reads a name its output cannot see.</summary>
    [MessageLogging(EventId = 21573, Level = LogLevel.Warning, Message = "Column '{columnName}' of table output '{outputName}' reads '{fieldName}', which that output cannot see: it sees its single inputs, its derived values and the columns of its row source")]
    public static partial IGenericMessage NameNotVisible(ILogger logger, string outputName, string columnName, string fieldName);

    /// <summary>Logs a transformation that states both results and table outputs.</summary>
    [MessageLogging(EventId = 21574, Level = LogLevel.Warning, Message = "A Built transformation has results or table outputs, not both; '{transformationName}' states both")]
    public static partial IGenericMessage ResultsAndTableOutputs(ILogger logger, string transformationName);

    /// <summary>Logs a table member stated for a transformation that has no table outputs.</summary>
    [MessageLogging(EventId = 21575, Level = LogLevel.Warning, Message = "'{member}' belongs to a transformation with table outputs; '{transformationName}' states results instead")]
    public static partial IGenericMessage TableMemberWithoutTableOutputs(ILogger logger, string transformationName, string member);

    /// <summary>Logs that a Built transformation was saved.</summary>
    [MessageLogging(EventId = 11560, Level = LogLevel.Information, Message = "Built transformation '{transformationName}' saved with {inputCount} input(s) and {resultCount} result(s)")]
    public static partial IGenericMessage Saved(ILogger logger, string transformationName, int inputCount, int resultCount);
}
