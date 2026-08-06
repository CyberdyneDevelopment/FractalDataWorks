using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging methods for AnnotationProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8960-8979
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class AnnotationProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Annotations (8960-8961)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading annotations fails.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to load annotations for '{dataSetName}'")]
    public static partial IGenericMessage LoadAnnotationsFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when loading annotations fails with exception.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to load annotations")]
    public static partial IGenericMessage LoadAnnotationsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Annotation (8962-8963)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating an annotation fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to create annotation for '{dataSetName}'")]
    public static partial IGenericMessage CreateAnnotationFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when creating an annotation fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to create annotation")]
    public static partial IGenericMessage CreateAnnotationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Annotation (8964-8965)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting an annotation fails.</summary>
    [MessageLogging(EventId = 71004, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to delete annotation '{annotationId}'")]
    public static partial IGenericMessage DeleteAnnotationFailed(
        ILogger logger,
        Guid annotationId);

    /// <summary>Logs when deleting an annotation fails with exception.</summary>
    [MessageLogging(EventId = 71005, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to delete annotation")]
    public static partial IGenericMessage DeleteAnnotationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Resolve Annotation (8966-8967)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when resolving an annotation fails.</summary>
    [MessageLogging(EventId = 71006, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to resolve annotation '{annotationId}'")]
    public static partial IGenericMessage ResolveAnnotationFailed(
        ILogger logger,
        Guid annotationId);

    /// <summary>Logs when resolving an annotation fails with exception.</summary>
    [MessageLogging(EventId = 71007, Level = LogLevel.Warning,
        Message = "AnnotationProvider: Failed to resolve annotation")]
    public static partial IGenericMessage ResolveAnnotationException(
        ILogger logger,
        Exception exception);
}
