using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for FieldMappingTransformProvider operations.
/// EventId range: 4200-4219
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class FieldMappingTransformProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Transforms (4200-4203)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the transforms for the field mapping are being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11063,
        Level = LogLevel.Trace,
        Message = "Loading transforms for field mapping")]
    public static partial IGenericMessage LoadingTransforms(ILogger logger);

    /// <summary>
    /// Logs that a number of transforms were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of transforms that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11064,
        Level = LogLevel.Debug,
        Message = "Loaded {count} transforms")]
    public static partial IGenericMessage LoadedTransforms(ILogger logger, int count);

    /// <summary>
    /// Logs that loading the transforms failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91035,
        Level = LogLevel.Warning,
        Message = "Failed to load transforms")]
    public static partial IGenericMessage LoadTransformsFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception was thrown while loading the transforms.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the transforms.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91036,
        Level = LogLevel.Warning,
        Message = "Failed to load transforms")]
    public static partial IGenericMessage LoadTransformsException(ILogger logger, Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Available Types (4204-4206)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the available transform types are being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11065,
        Level = LogLevel.Trace,
        Message = "Loading available transform types")]
    public static partial IGenericMessage LoadingAvailableTypes(ILogger logger);

    /// <summary>
    /// Logs that a number of available transform types were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of available transform types that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11066,
        Level = LogLevel.Debug,
        Message = "Loaded {count} available transform types")]
    public static partial IGenericMessage LoadedAvailableTypes(ILogger logger, int count);

    /// <summary>
    /// Logs that loading the available transform types failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91037,
        Level = LogLevel.Warning,
        Message = "Failed to load available transform types")]
    public static partial IGenericMessage LoadAvailableTypesFailed(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Save Transform (4207-4210)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the transform of the named type is being saved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformType">The type of the transform being saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11067,
        Level = LogLevel.Trace,
        Message = "Saving transform '{transformType}'")]
    public static partial IGenericMessage SavingTransform(ILogger logger, string transformType);

    /// <summary>
    /// Logs that the transform of the named type was saved.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformType">The type of the transform that was saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11068,
        Level = LogLevel.Information,
        Message = "Saved transform '{transformType}'")]
    public static partial IGenericMessage SavedTransform(ILogger logger, string transformType);

    /// <summary>
    /// Logs that saving the transform of the named type failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformType">The type of the transform that failed to save.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91038,
        Level = LogLevel.Warning,
        Message = "Failed to save transform '{transformType}'")]
    public static partial IGenericMessage SaveTransformFailed(ILogger logger, string transformType);

    /// <summary>
    /// Logs that an exception was thrown while saving the transform.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while saving the transform.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91039,
        Level = LogLevel.Warning,
        Message = "Failed to save transform")]
    public static partial IGenericMessage SaveTransformException(ILogger logger, Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Transform (4211-4214)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the transform with the given identifier is being deleted.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformId">The identifier of the transform being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11069,
        Level = LogLevel.Trace,
        Message = "Deleting transform '{transformId}'")]
    public static partial IGenericMessage DeletingTransform(ILogger logger, string transformId);

    /// <summary>
    /// Logs that the transform with the given identifier was deleted.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformId">The identifier of the transform that was deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11070,
        Level = LogLevel.Information,
        Message = "Deleted transform '{transformId}'")]
    public static partial IGenericMessage DeletedTransform(ILogger logger, string transformId);

    /// <summary>
    /// Logs that deleting the transform with the given identifier failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformId">The identifier of the transform that failed to delete.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91040,
        Level = LogLevel.Warning,
        Message = "Failed to delete transform '{transformId}'")]
    public static partial IGenericMessage DeleteTransformFailed(ILogger logger, string transformId);

    /// <summary>
    /// Logs that an exception was thrown while deleting the transform.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while deleting the transform.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91041,
        Level = LogLevel.Warning,
        Message = "Failed to delete transform")]
    public static partial IGenericMessage DeleteTransformException(ILogger logger, Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Reorder Transforms (4215-4217)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the transforms are being reordered.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11071,
        Level = LogLevel.Trace,
        Message = "Reordering transforms")]
    public static partial IGenericMessage ReorderingTransforms(ILogger logger);

    /// <summary>
    /// Logs that the transforms were reordered.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11072,
        Level = LogLevel.Information,
        Message = "Reordered transforms")]
    public static partial IGenericMessage ReorderedTransforms(ILogger logger);

    /// <summary>
    /// Logs that reordering the transforms failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91042,
        Level = LogLevel.Warning,
        Message = "Failed to reorder transforms")]
    public static partial IGenericMessage ReorderTransformsFailed(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Select / Add (4218-4219)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the transform with the given identifier is being selected.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformId">The identifier of the transform being selected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11073,
        Level = LogLevel.Trace,
        Message = "Selecting transform '{transformId}'")]
    public static partial IGenericMessage SelectingTransform(ILogger logger, string transformId);

    /// <summary>
    /// Logs that a transform of the named type is being added.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="transformType">The type of the transform being added.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11074,
        Level = LogLevel.Trace,
        Message = "Adding transform '{transformType}'")]
    public static partial IGenericMessage AddingTransform(ILogger logger, string transformType);
}
