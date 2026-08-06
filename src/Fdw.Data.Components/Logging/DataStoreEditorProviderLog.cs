using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging methods for DataStoreEditorProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8952-8969
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataStoreEditorProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Initialisation (8952-8953)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading connections for the editor fails.</summary>
    [MessageLogging(EventId = 91025, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Failed to load connections list")]
    public static partial IGenericMessage LoadConnectionsFailed(
        ILogger logger);

    /// <summary>Logs when loading connections fails with exception.</summary>
    [MessageLogging(EventId = 91026, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Exception loading connections list")]
    public static partial IGenericMessage LoadConnectionsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // DataStore Types (8954-8955)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading DataStore types fails.</summary>
    [MessageLogging(EventId = 91027, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Failed to load DataStore types")]
    public static partial IGenericMessage LoadDataStoreTypesFailed(
        ILogger logger);

    /// <summary>Logs when loading DataStore types fails with exception.</summary>
    [MessageLogging(EventId = 91028, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Exception loading DataStore types")]
    public static partial IGenericMessage LoadDataStoreTypesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Capabilities (8956-8957)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading capabilities for a connection type fails.</summary>
    [MessageLogging(EventId = 91029, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Failed to load capabilities for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage LoadCapabilitiesFailed(
        ILogger logger,
        string connectionTypeName);

    /// <summary>Logs when loading capabilities fails with exception.</summary>
    [MessageLogging(EventId = 91030, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Exception loading capabilities for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage LoadCapabilitiesException(
        ILogger logger,
        Exception exception,
        string connectionTypeName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Existing (8958-8959)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading an existing DataStore for editing fails.</summary>
    [MessageLogging(EventId = 91031, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Failed to load existing DataStore '{dataStoreName}' for editing")]
    public static partial IGenericMessage LoadExistingFailed(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when loading an existing DataStore for editing fails with exception.</summary>
    [MessageLogging(EventId = 91032, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Exception loading existing DataStore '{dataStoreName}'")]
    public static partial IGenericMessage LoadExistingException(
        ILogger logger,
        Exception exception,
        string dataStoreName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Save (8960-8961)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when saving a DataStore fails.</summary>
    [MessageLogging(EventId = 91033, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Failed to save DataStore '{dataStoreName}'")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger,
        string dataStoreName);

    /// <summary>Logs when saving a DataStore fails with exception.</summary>
    [MessageLogging(EventId = 91034, Level = LogLevel.Warning,
        Message = "DataStoreEditorProvider: Exception saving DataStore '{dataStoreName}'")]
    public static partial IGenericMessage SaveException(
        ILogger logger,
        Exception exception,
        string dataStoreName);
}
