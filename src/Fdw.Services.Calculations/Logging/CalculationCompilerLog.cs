using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Calculations.Logging;

/// <summary>
/// MessageLogging for compiling a calculation array into one executable delegate.
/// EventIds 12000-12002.
/// </summary>
[MessageLoggingTypeCode("CALCULATIONS")]
internal static partial class CalculationCompilerLog
{
    /// <summary>Traces that an array is about to be validated and compiled.</summary>
    [MessageLogging(EventId = 12000, Level = LogLevel.Trace, Message = "Compiling calculation array '{arrayName}' with {resultCount} result(s)")]
    public static partial IGenericMessage CompileStarted(ILogger logger, string arrayName, int resultCount);

    /// <summary>Records that an array compiled; it happens once per array per run, not per record.</summary>
    [MessageLogging(EventId = 12001, Level = LogLevel.Information, Message = "Compiled calculation array '{arrayName}': {resultCount} result(s), {emittedCount} written, {nodeCount} node(s)")]
    public static partial IGenericMessage CompileCompleted(ILogger logger, string arrayName, int resultCount, int emittedCount, int nodeCount);

    /// <summary>Warns that an array was refused; the refusal itself is returned as a coded result.</summary>
    [MessageLogging(EventId = 12002, Level = LogLevel.Warning, Message = "Calculation array '{arrayName}' was refused: {code}")]
    public static partial IGenericMessage CompileRefused(ILogger logger, string arrayName, string? code);
}
