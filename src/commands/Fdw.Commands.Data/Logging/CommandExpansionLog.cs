using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for the expansion of a data command into simpler data commands
/// (<see cref="Abstractions.IExpandableDataCommand"/>).
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class CommandExpansionLog
{
    /// <summary>Traces a data command being expanded.</summary>
    [MessageLogging(EventId = 11014, Level = LogLevel.Trace,
        Message = "[{commandType}] Expanded into {commandCount} commands")]
    public static partial IGenericMessage Expanded(ILogger logger, string commandType, int commandCount);

    /// <summary>Logs a row that has no value for a key column, so the command for it cannot be filtered on its key.</summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Warning,
        Message = "[{commandType}] A row has no value for key column '{keyColumn}'")]
    public static partial IGenericMessage KeyValueMissing(ILogger logger, string commandType, string keyColumn);

    /// <summary>Logs a command whose options or container the delete-and-insert composition cannot honour.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning,
        Message = "[{commandType}] This kind has no translator for it, and the delete and insert that stand in for it cannot be used: {reason}")]
    public static partial IGenericMessage NotComposable(ILogger logger, string commandType, string? reason);
}
