using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataContainerDetailLoader operations.
/// EventId range: 7045-7059
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataContainerDetailLoaderLog
{
    /// <summary>
    /// Logs that field loading has started for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerRowId">The row identifier of the container whose fields are being loaded.</param>
    /// <param name="typeId">The type identifier of the container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11021, Level = LogLevel.Trace,
        Message = "[DataContainerDetailLoader] Starting field load for container {containerRowId} (typeId='{typeId}')")]
    public static partial IGenericMessage LoadFieldsStarted(ILogger logger, string containerRowId, string typeId);

    /// <summary>
    /// Logs that field loading completed for a container, reporting the number of fields loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="fieldCount">The number of fields loaded for the container.</param>
    /// <param name="containerRowId">The row identifier of the container whose fields were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11022, Level = LogLevel.Information,
        Message = "[DataContainerDetailLoader] Loaded {fieldCount} fields for container {containerRowId}")]
    public static partial IGenericMessage LoadFieldsCompleted(ILogger logger, int fieldCount, string containerRowId);

    /// <summary>
    /// Logs that field loading failed for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the field load to fail.</param>
    /// <param name="containerRowId">The row identifier of the container whose fields failed to load.</param>
    /// <param name="error">The error describing why the field load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "[DataContainerDetailLoader] Field load failed for container {containerRowId}: {error}")]
    public static partial IGenericMessage LoadFieldsFailed(ILogger logger, Exception exception, string containerRowId, string error);

    /// <summary>
    /// Logs that key loading has started for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerRowId">The row identifier of the container whose keys are being loaded.</param>
    /// <param name="typeId">The type identifier of the container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11023, Level = LogLevel.Trace,
        Message = "[DataContainerDetailLoader] Starting key load for container {containerRowId} (typeId='{typeId}')")]
    public static partial IGenericMessage LoadKeysStarted(ILogger logger, string containerRowId, string typeId);

    /// <summary>
    /// Logs that key loading completed for a container, reporting the number of keys loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="keyCount">The number of keys loaded for the container.</param>
    /// <param name="containerRowId">The row identifier of the container whose keys were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11024, Level = LogLevel.Information,
        Message = "[DataContainerDetailLoader] Loaded {keyCount} keys for container {containerRowId}")]
    public static partial IGenericMessage LoadKeysCompleted(ILogger logger, int keyCount, string containerRowId);

    /// <summary>
    /// Logs that key loading failed for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the key load to fail.</param>
    /// <param name="containerRowId">The row identifier of the container whose keys failed to load.</param>
    /// <param name="error">The error describing why the key load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "[DataContainerDetailLoader] Key load failed for container {containerRowId}: {error}")]
    public static partial IGenericMessage LoadKeysFailed(ILogger logger, Exception exception, string containerRowId, string error);

    /// <summary>
    /// Logs that the field cache was hit for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerRowId">The row identifier of the container whose fields were served from cache.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11025, Level = LogLevel.Trace,
        Message = "[DataContainerDetailLoader] Cache hit for container {containerRowId} fields")]
    public static partial IGenericMessage FieldsCacheHit(ILogger logger, string containerRowId);

    /// <summary>
    /// Logs that the key cache was hit for a container.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="containerRowId">The row identifier of the container whose keys were served from cache.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11026, Level = LogLevel.Trace,
        Message = "[DataContainerDetailLoader] Cache hit for container {containerRowId} keys")]
    public static partial IGenericMessage KeysCacheHit(ILogger logger, string containerRowId);

    /// <summary>
    /// Logs that a container's typeId is not an MsSql family, so empty fields are returned.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="typeId">The type identifier that is not an MsSql family.</param>
    /// <param name="containerRowId">The row identifier of the container for which empty fields are returned.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61005, Level = LogLevel.Warning,
        Message = "[DataContainerDetailLoader] typeId '{typeId}' is not an MsSql family — returning empty fields for container {containerRowId}")]
    public static partial IGenericMessage FieldTypeNotSupported(ILogger logger, string typeId, string containerRowId);

    /// <summary>
    /// Logs that a container's typeId is not an MsSql family, so empty keys are returned.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="typeId">The type identifier that is not an MsSql family.</param>
    /// <param name="containerRowId">The row identifier of the container for which empty keys are returned.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 61006, Level = LogLevel.Warning,
        Message = "[DataContainerDetailLoader] typeId '{typeId}' is not an MsSql family — returning empty keys for container {containerRowId}")]
    public static partial IGenericMessage KeyTypeNotSupported(ILogger logger, string typeId, string containerRowId);
}
