using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Operations.Components.Logging;

/// <summary>
/// MessageLogging methods for the AuditProvider headless component.
/// EventId range: 4203-4209
/// </summary>
[MessageLoggingTypeCode("COMPONENTS4")]
public static partial class AuditProviderLog
{
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "AuditProvider: Loading execution history")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    [MessageLogging(EventId = 11001, Level = LogLevel.Trace,
        Message = "AuditProvider: Loaded {count} execution entries")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int count);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "AuditProvider: Failed to load execution history")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "AuditProvider: Failed to load execution history")]
    public static partial IGenericMessage LoadException(ILogger logger, System.Exception exception);

    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "AuditProvider: Filter changed — itemType='{itemType}', state='{state}'")]
    public static partial IGenericMessage FilterChanged(ILogger logger, string itemType, string state);
}
