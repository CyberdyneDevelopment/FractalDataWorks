using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Aui.Logging;

/// <summary>
/// MessageLogging for Agent User Interface (AUI) operations.
/// EventId range: 7100-7199
/// </summary>
[MessageLoggingTypeCode("AUI")]
public static partial class AuiLog
{
    /// <summary> Logs that an AUI manifest was requested. </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "AUI manifest requested for route '{route}' by user '{userId}'")]
    public static partial IGenericMessage ManifestRequested(
        ILogger logger,
        string route,
        Guid userId);

    /// <summary> Logs that an AUI action was executed. </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "AUI action '{actionName}' executed by user '{userId}'")]
    public static partial IGenericMessage ActionExecuted(
        ILogger logger,
        string actionName,
        Guid userId);

    /// <summary> Logs that an AUI operation failed. </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "AUI operation failed: {errorMessage}")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        Exception ex,
        string errorMessage);

    /// <summary> Logs that an AUI provider failed to return a manifest. </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "AUI provider '{providerName}' failed: {reason}")]
    public static partial IGenericMessage ProviderFailed(
        ILogger logger,
        string providerName,
        string reason);

    /// <summary> Logs that an agent browser was detected. </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Agent browser detected. Intercepting request for '{path}'")]
    public static partial IGenericMessage AgentDetected(
        ILogger logger,
        string path);
}
