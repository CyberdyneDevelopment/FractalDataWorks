using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for CalculationConfigurationProvider gateway operations.
/// EventId range: 4531-4545
/// </summary>
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationConfigurationProviderLog
{
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace, Message = "Reading entity rows for calculation '{name}'")]
    public static partial IGenericMessage GetByNameTrace(ILogger logger, string name);

    [MessageLogging(EventId = 11017, Level = LogLevel.Trace, Message = "Reading entity rows for calculation id={id}")]
    public static partial IGenericMessage GetByIdTrace(ILogger logger, System.Guid id);

    [MessageLogging(EventId = 11018, Level = LogLevel.Trace, Message = "Reading all calculation entity rows")]
    public static partial IGenericMessage GetAllTrace(ILogger logger);

    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "Failed to read calculation entity rows")]
    public static partial IGenericMessage GetFailed(ILogger logger, Exception ex);

    [MessageLogging(EventId = 11019, Level = LogLevel.Trace, Message = "Reading input rows for calculationEntityId={calculationEntityId}")]
    public static partial IGenericMessage GetInputsTrace(ILogger logger, System.Guid calculationEntityId);

    [MessageLogging(EventId = 71003, Level = LogLevel.Error, Message = "Failed to read input rows for calculationEntityId={calculationEntityId}")]
    public static partial IGenericMessage GetInputsFailed(ILogger logger, System.Guid calculationEntityId, Exception ex);

    [MessageLogging(EventId = 11020, Level = LogLevel.Trace, Message = "Inserting entity row for calculation '{name}'")]
    public static partial IGenericMessage InsertEntityTrace(ILogger logger, string name);

    [MessageLogging(EventId = 71004, Level = LogLevel.Error, Message = "Failed to insert entity row for calculation '{name}'")]
    public static partial IGenericMessage InsertEntityFailed(ILogger logger, string name, Exception ex);

    [MessageLogging(EventId = 11021, Level = LogLevel.Trace, Message = "Inserting input row for calculation id={id}, alias='{alias}'")]
    public static partial IGenericMessage InsertInputTrace(ILogger logger, System.Guid id, string alias);

    [MessageLogging(EventId = 71005, Level = LogLevel.Error, Message = "Failed to insert input row for calculation id={id}, alias='{alias}'")]
    public static partial IGenericMessage InsertInputFailed(ILogger logger, System.Guid id, string alias, Exception ex);

    [MessageLogging(EventId = 11022, Level = LogLevel.Trace, Message = "Retiring current entity row for calculation id={id}")]
    public static partial IGenericMessage RetireEntityTrace(ILogger logger, System.Guid id);

    [MessageLogging(EventId = 71006, Level = LogLevel.Error, Message = "Failed to retire current entity row for calculation id={id}")]
    public static partial IGenericMessage RetireEntityFailed(ILogger logger, System.Guid id, Exception ex);

    [MessageLogging(EventId = 11023, Level = LogLevel.Trace, Message = "Retiring current input rows for calculationEntityId={calculationEntityId}")]
    public static partial IGenericMessage RetireInputsTrace(ILogger logger, System.Guid calculationEntityId);

    [MessageLogging(EventId = 71007, Level = LogLevel.Error, Message = "Failed to retire current input rows for calculationEntityId={calculationEntityId}")]
    public static partial IGenericMessage RetireInputsFailed(ILogger logger, System.Guid calculationEntityId, Exception ex);
}
