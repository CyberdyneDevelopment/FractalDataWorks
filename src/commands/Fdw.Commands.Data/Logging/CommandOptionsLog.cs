using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for the reasons a command's options are inconsistent: an upsert's, a soft delete's and a bulk update's.
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
}
