using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for container discovery operations.
/// EventId range: 7000-7019
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DiscoveryLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Discovery Method Resolution (7000-7004)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a discovery method has been resolved from configuration.
    /// </summary>
    [MessageLogging(
        EventId = 11223,
        Level = LogLevel.Information,
        Message = "[Discovery] Resolved discovery method '{methodName}' for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiscoveryMethodResolved(ILogger logger, string methodName, string dataStoreName);

    /// <summary>
    /// Logs that a discovery method was not found by name.
    /// </summary>
    [MessageLogging(
        EventId = 31036,
        Level = LogLevel.Warning,
        Message = "[Discovery] Discovery method '{methodName}' not found in DiscoveryMethods collection")]
    public static partial IGenericMessage DiscoveryMethodNotFound(ILogger logger, string methodName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Auto Discovery (7005-7009)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that auto-discovery is starting for a DataStore.
    /// </summary>
    [MessageLogging(
        EventId = 11224,
        Level = LogLevel.Information,
        Message = "[Discovery] Starting auto-discovery for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage AutoDiscoveryStarted(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs the number of containers discovered via auto-discovery.
    /// </summary>
    [MessageLogging(
        EventId = 11225,
        Level = LogLevel.Information,
        Message = "[Discovery] Auto-discovery found {containerCount} containers in DataStore '{dataStoreName}'")]
    public static partial IGenericMessage AutoDiscoveryCompleted(ILogger logger, int containerCount, string dataStoreName);

    /// <summary>
    /// Logs that auto-discovery is not supported for the DataStore type.
    /// </summary>
    [MessageLogging(
        EventId = 61021,
        Level = LogLevel.Warning,
        Message = "[Discovery] Auto-discovery is not supported for DataStore '{dataStoreName}' (type '{dataStoreType}')")]
    public static partial IGenericMessage AutoDiscoveryNotSupported(ILogger logger, string dataStoreName, string dataStoreType);

    // ═══════════════════════════════════════════════════════════════════════════
    // File Discovery (7010-7014)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that file-based discovery is starting.
    /// </summary>
    [MessageLogging(
        EventId = 11226,
        Level = LogLevel.Information,
        Message = "[Discovery] Starting file discovery from '{filePath}' (format: '{fileFormat}')")]
    public static partial IGenericMessage FileDiscoveryStarted(ILogger logger, string filePath, string fileFormat);

    /// <summary>
    /// Logs that file-based discovery completed successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11227,
        Level = LogLevel.Information,
        Message = "[Discovery] File discovery loaded {containerCount} containers from '{filePath}'")]
    public static partial IGenericMessage FileDiscoveryCompleted(ILogger logger, int containerCount, string filePath);

    /// <summary>
    /// Logs that the file for discovery could not be found.
    /// </summary>
    [MessageLogging(
        EventId = 31037,
        Level = LogLevel.Error,
        Message = "[Discovery] Discovery file not found: '{filePath}'")]
    public static partial IGenericMessage FileDiscoveryFileNotFound(ILogger logger, string filePath);

    // ═══════════════════════════════════════════════════════════════════════════
    // Manual Discovery (7015-7016)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that a container was manually defined.
    /// </summary>
    [MessageLogging(
        EventId = 11228,
        Level = LogLevel.Debug,
        Message = "[Discovery] Container '{containerName}' manually defined with {fieldCount} fields")]
    public static partial IGenericMessage ManualContainerDefined(ILogger logger, string containerName, int fieldCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation (7017-7019)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs a discovery validation failure.
    /// </summary>
    [MessageLogging(
        EventId = 20001,
        Level = LogLevel.Error,
        Message = "[Discovery] Validation failed for discovery method '{methodName}': {validationErrors}")]
    public static partial IGenericMessage DiscoveryValidationFailed(ILogger logger, string methodName, string validationErrors);

    /// <summary>
    /// Logs a discovery operation failure with exception.
    /// </summary>
    [MessageLogging(
        EventId = 71035,
        Level = LogLevel.Error,
        Message = "[Discovery] Discovery operation failed for DataStore '{dataStoreName}'")]
    public static partial IGenericMessage DiscoveryOperationFailed(ILogger logger, System.Exception ex, string dataStoreName);
}
