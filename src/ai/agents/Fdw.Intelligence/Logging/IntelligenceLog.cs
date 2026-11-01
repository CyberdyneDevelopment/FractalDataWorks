using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Intelligence.Logging;

/// <summary>
/// MessageLogging for Intelligence Service operations.
/// EventId range: 7060-7079
/// </summary>
[MessageLoggingTypeCode("INTELLIGENCE")]
public static partial class IntelligenceLog
{
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Memory entry recorded: {entryId}")]
    public static partial IGenericMessage MemoryRecorded(
        ILogger logger,
        Guid entryId);

    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "Memory recall for query '{query}' returned {count} results")]
    public static partial IGenericMessage MemoryRecalled(
        ILogger logger,
        string query,
        int count);

    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "Sidecar trigger detected in stream: '{content}'")]
    public static partial IGenericMessage TriggerDetected(
        ILogger logger,
        string content);

    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Sidecar injected {count} recalled memories into stream")]
    public static partial IGenericMessage RecallInjected(
        ILogger logger,
        int count);

    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Intelligence operation failed: {errorMessage}")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        string errorMessage);
}
