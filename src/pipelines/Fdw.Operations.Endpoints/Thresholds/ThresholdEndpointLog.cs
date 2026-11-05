using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>
/// MessageLogging for the threshold endpoints (FDW-3948): creating, replacing and deleting a threshold, and attaching and detaching it.
/// EventId range: 7300-7309.
/// </summary>
[MessageLoggingTypeCode("THRESHOLDS")]
public static partial class ThresholdEndpointLog
{
    /// <summary>Logs a threshold being created.</summary>
    [MessageLogging(EventId = 7300, Level = LogLevel.Information, Message = "Creating {type} threshold '{name}' with {rules} rule(s)")]
    public static partial IGenericMessage Creating(ILogger logger, string type, string name, int rules);

    /// <summary>Logs a threshold being replaced.</summary>
    [MessageLogging(EventId = 7301, Level = LogLevel.Information, Message = "Replacing threshold '{name}' with {rules} rule(s)")]
    public static partial IGenericMessage Replacing(ILogger logger, string name, int rules);

    /// <summary>Logs a threshold being deleted.</summary>
    [MessageLogging(EventId = 7302, Level = LogLevel.Information, Message = "Deleting threshold '{name}'")]
    public static partial IGenericMessage Deleting(ILogger logger, string name);

    /// <summary>Logs a threshold that was saved and cannot be read back.</summary>
    [MessageLogging(EventId = 7303, Level = LogLevel.Error, Message = "Threshold '{name}' was saved and cannot be read back")]
    public static partial IGenericMessage SavedButUnreadable(ILogger logger, string name);

    /// <summary>Logs a threshold being attached to a subject.</summary>
    [MessageLogging(EventId = 7304, Level = LogLevel.Information, Message = "Attaching threshold '{name}' to {kind} '{subject}'")]
    public static partial IGenericMessage Attaching(ILogger logger, string name, string kind, string subject);

    /// <summary>Logs the thresholds of a subject being read.</summary>
    [MessageLogging(EventId = 7306, Level = LogLevel.Debug, Message = "Reading the thresholds attached to {kind} '{subject}'")]
    public static partial IGenericMessage ReadingSubject(ILogger logger, string kind, string subject);

    /// <summary>Logs a threshold being detached from a subject.</summary>
    [MessageLogging(EventId = 7305, Level = LogLevel.Information, Message = "Detaching threshold '{name}' from {kind} '{subject}'")]
    public static partial IGenericMessage Detaching(ILogger logger, string name, string kind, string subject);
}
