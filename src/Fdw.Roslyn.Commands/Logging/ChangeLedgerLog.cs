using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging methods for change ledger operations.
/// </summary>
[MessageLoggingTypeCode("LEDGER")]
public static partial class ChangeLedgerLog
{
    /// <summary>Debug: a ledger entry was recorded.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Ledger entry {sequence} recorded for command '{commandName}'")]
    public static partial IGenericMessage LedgerEntryRecorded(ILogger logger, int sequence, string commandName);

    /// <summary>Debug: the ledger was cleared.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Change ledger cleared")]
    public static partial IGenericMessage LedgerCleared(ILogger logger);

    /// <summary>Info: the migration guide was written successfully.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "Migration guide written to '{path}' with {entryCount} entries")]
    public static partial IGenericMessage MigrationGuideWritten(ILogger logger, string path, int entryCount);

    /// <summary>Error: writing the migration guide failed.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Failed to write migration guide to '{path}'")]
    public static partial IGenericMessage MigrationGuideWriteFailed(ILogger logger, Exception exception, string path);
}
