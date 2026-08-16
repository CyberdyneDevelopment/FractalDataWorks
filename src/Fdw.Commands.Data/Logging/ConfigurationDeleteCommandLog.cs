using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="ConfigurationDeleteCommand"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class ConfigurationDeleteCommandLog
{
    /// <summary>Traces a single-row configuration delete command being constructed.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "[ConfigurationDeleteCommand] Created for logical id {logicalId}")]
    public static partial IGenericMessage CommandCreated(ILogger logger, Guid logicalId);

    /// <summary>Traces an owner-scoped configuration delete command being constructed.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "[ConfigurationDeleteCommand] Created scoped delete for owner {ownerLogicalId} via FK column '{ownerForeignKeyColumn}'")]
    public static partial IGenericMessage ScopedCommandCreated(ILogger logger, Guid ownerLogicalId, string ownerForeignKeyColumn);

    /// <summary>
    /// Logs the defect condition immediately before <see cref="ConfigurationDeleteCommand"/> throws
    /// <see cref="ArgumentException"/> for a missing owner foreign key column. See the logging-pass
    /// report — the throw itself is left in place.
    /// </summary>
    [MessageLogging(EventId = 20000, Level = LogLevel.Error,
        Message = "[ConfigurationDeleteCommand] Owner foreign key column is required for a scoped configuration delete and was null or empty")]
    public static partial IGenericMessage OwnerForeignKeyColumnMissing(ILogger logger);
}
