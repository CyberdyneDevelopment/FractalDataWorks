using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Abstractions.Logging;

/// <summary>MessageLogging for reading a set-based write's answer of unmatched keys (FDW-4988): each way the result sets cannot be one is its own code.</summary>
[MessageLoggingTypeCode("DATAABSTRACTIONS")]
public static partial class BulkWriteAnswerLog
{
    /// <summary>Logs an answer whose batch did not select a summary set and a key set.</summary>
    [MessageLogging(EventId = 21003, Level = LogLevel.Error,
        Message = "Command '{commandType}' answered {sets} result set(s); a write that reports its unmatched keys answers a summary set and a set of keys")]
    public static partial IGenericMessage SetsNotTwo(ILogger logger, string commandType, int sets);

    /// <summary>Logs an answer whose summary set is not one row.</summary>
    [MessageLogging(EventId = 21004, Level = LogLevel.Error,
        Message = "Command '{commandType}' answered a summary set of {rows} row(s); it is one row")]
    public static partial IGenericMessage SummaryNotOneRow(ILogger logger, string commandType, int rows);

    /// <summary>Logs an answer whose summary row lacks a count, or holds one that is not a whole number.</summary>
    [MessageLogging(EventId = 21005, Level = LogLevel.Error,
        Message = "Command '{commandType}' answered a summary row whose column '{column}' is missing or not a whole number")]
    public static partial IGenericMessage SummaryColumnUnreadable(ILogger logger, string commandType, string column);

    /// <summary>Logs an answer with a count below zero.</summary>
    [MessageLogging(EventId = 21006, Level = LogLevel.Error,
        Message = "Command '{commandType}' answered {affected} row(s) affected and {unmatched} unmatched key(s); neither can be below zero")]
    public static partial IGenericMessage CountNegative(ILogger logger, string commandType, long affected, long unmatched);

    /// <summary>Logs an answer that carries more keys than the command asked for.</summary>
    [MessageLogging(EventId = 21007, Level = LogLevel.Error,
        Message = "Command '{commandType}' answered {keys} unmatched key(s), more than the {limit} it asked for")]
    public static partial IGenericMessage SampleOverLimit(ILogger logger, string commandType, int keys, int limit);

    /// <summary>Logs an answer that carries more keys than it counted unmatched.</summary>
    [MessageLogging(EventId = 21008, Level = LogLevel.Error,
        Message = "Command '{commandType}' answered {keys} unmatched key(s) but counted only {unmatched}")]
    public static partial IGenericMessage SampleOverCount(ILogger logger, string commandType, int keys, long unmatched);
}
