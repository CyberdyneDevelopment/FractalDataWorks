using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for compiling and running a calculation set (FDW-1227).
/// EventIds 12600-12603.
/// </summary>
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationSetLog
{
    /// <summary>Traces that a set is about to be checked and compiled.</summary>
    [MessageLogging(EventId = 12600, Level = LogLevel.Trace, Message = "Compiling calculation set '{setName}' with {outputCount} output(s)")]
    public static partial IGenericMessage CompileStarted(ILogger logger, string setName, int outputCount);

    /// <summary>Records that a set compiled; it happens once per set per run.</summary>
    [MessageLogging(EventId = 12601, Level = LogLevel.Information, Message = "Compiled calculation set '{setName}': {outputCount} output(s), {formulaCount} formula(s) compiled")]
    public static partial IGenericMessage CompileCompleted(ILogger logger, string setName, int outputCount, int formulaCount);

    /// <summary>Warns that a set was refused; the refusal itself is returned as a coded result.</summary>
    [MessageLogging(EventId = 12602, Level = LogLevel.Warning, Message = "Calculation set '{setName}' was refused: {code}")]
    public static partial IGenericMessage CompileRefused(ILogger logger, string setName, string? code);

    /// <summary>Records that a set ran.</summary>
    [MessageLogging(EventId = 12603, Level = LogLevel.Information, Message = "Calculation set '{setName}' wrote {rowCount} row(s) across {outputCount} output(s)")]
    public static partial IGenericMessage Evaluated(ILogger logger, string setName, int rowCount, int outputCount);
}
