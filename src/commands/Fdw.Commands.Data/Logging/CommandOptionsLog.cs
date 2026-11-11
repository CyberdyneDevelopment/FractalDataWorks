using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for the reasons a command's options are inconsistent: an upsert's, a soft delete's, a bulk update's and a bulk
/// delete's.
/// Each options class answers its first problem as one of these messages, or nothing when it is consistent.
/// </summary>
/// <remarks>
/// Why messages and not words: a problem reader is chained (the options' own problem, then the translator's), and a chain
/// of words reads as a fallback; a chain of messages is a chain of failures, and the caller hands the one it gets to the
/// failure it returns (FDW-3580).
/// </remarks>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class CommandOptionsLog
{
    /// <summary>Logs the first thing wrong with an upsert's options.</summary>
    [MessageLogging(EventId = 62023, Level = LogLevel.Error,
        Message = "Upsert options are inconsistent: {reason}")]
    public static partial IGenericMessage UpsertOptionsInconsistent(ILogger logger, string reason);

    /// <summary>Logs the first thing wrong with a soft delete's options.</summary>
    [MessageLogging(EventId = 62024, Level = LogLevel.Error,
        Message = "Soft-delete options are inconsistent: {reason}")]
    public static partial IGenericMessage SoftDeleteOptionsInconsistent(ILogger logger, string reason);

    /// <summary>Logs the first thing wrong with a bulk update's options.</summary>
    [MessageLogging(EventId = 62025, Level = LogLevel.Error,
        Message = "Bulk update options are inconsistent: {reason}")]
    public static partial IGenericMessage BulkUpdateOptionsInconsistent(ILogger logger, string reason);

    /// <summary>Logs the first thing wrong with a bulk delete's options.</summary>
    [MessageLogging(EventId = 62026, Level = LogLevel.Error,
        Message = "Bulk delete options are inconsistent: {reason}")]
    public static partial IGenericMessage BulkDeleteOptionsInconsistent(ILogger logger, string reason);

    /// <summary>Logs a write that reports its unmatched keys with a sample size below zero (FDW-4988).</summary>
    [MessageLogging(EventId = 62027, Level = LogLevel.Error,
        Message = "Command '{commandType}' asks for {sample} unmatched key(s); a sample is at least 0 (0 answers only the count)")]
    public static partial IGenericMessage UnmatchedKeySampleNegative(ILogger logger, string commandType, int sample);
}
