using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Resiliency.PrimaryBackup;

/// <summary>
/// MessageLogging methods for PrimaryBackup resiliency strategy.
/// </summary>
[ExcludeFromCodeCoverage]
[MessageLoggingTypeCode("PRIMARYBACKUP")]
public static partial class PrimaryBackupLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // PrimaryBackup Strategy Events (7110-7119)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs when the wrong configuration type is passed to PrimaryBackup.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Error,
        Message = "PrimaryBackup received wrong configuration type: expected PrimaryBackupResiliencyConfiguration, got '{configType}'")]
    public static partial IGenericMessage WrongConfigurationType(
        ILogger logger,
        string configType);

    /// <summary>
    /// Logs when the execution context is not IPrimaryBackupResiliencyContext.
    /// </summary>
    [MessageLogging(
        EventId = 21001,
        Level = LogLevel.Error,
        Message = "PrimaryBackup requires IPrimaryBackupResiliencyContext: executionId={executionId}, contextType='{contextType}'")]
    public static partial IGenericMessage WrongContextType(
        ILogger logger,
        Guid executionId,
        string contextType);

    /// <summary>
    /// Logs when the refresh schedule toggle fails after backup activation (non-fatal).
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Warning,
        Message = "PrimaryBackup: Failed to toggle refresh schedule: executionId={executionId}, scheduleId={scheduleId}, reason='{reason}'")]
    public static partial IGenericMessage ScheduleToggleFailed(
        ILogger logger,
        Guid executionId,
        Guid scheduleId,
        string reason);
}
