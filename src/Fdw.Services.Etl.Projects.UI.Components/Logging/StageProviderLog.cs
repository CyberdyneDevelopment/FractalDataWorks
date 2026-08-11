using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Etl.Projects.UI.Components.Logging;

/// <summary>
/// MessageLogging methods for StageProvider operations.
/// EventId range: 8800-8807
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS2")]
public static partial class StageProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Stages (8800-8801)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the stages list fails.</summary>
    [MessageLogging(EventId = 71022, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to load stages for project '{projectId}'")]
    public static partial IGenericMessage LoadStagesFailed(
        ILogger logger,
        string projectId);

    /// <summary>Logs when loading the stages list fails with exception.</summary>
    [MessageLogging(EventId = 71023, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to load stages list")]
    public static partial IGenericMessage LoadStagesException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Stage (8802-8803)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a stage fails.</summary>
    [MessageLogging(EventId = 71024, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to create stage")]
    public static partial IGenericMessage StageCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a stage fails with exception.</summary>
    [MessageLogging(EventId = 71025, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to create stage")]
    public static partial IGenericMessage StageCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Stage (8804-8805)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a stage fails.</summary>
    [MessageLogging(EventId = 71026, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to update stage '{stageId}'")]
    public static partial IGenericMessage StageUpdateFailed(
        ILogger logger,
        string stageId);

    /// <summary>Logs when updating a stage fails with exception.</summary>
    [MessageLogging(EventId = 71027, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to update stage")]
    public static partial IGenericMessage StageUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Stage (8806-8807)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a stage fails.</summary>
    [MessageLogging(EventId = 71028, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to delete stage '{stageId}'")]
    public static partial IGenericMessage StageDeleteFailed(
        ILogger logger,
        string stageId);

    /// <summary>Logs when deleting a stage fails with exception.</summary>
    [MessageLogging(EventId = 71029, Level = LogLevel.Warning,
        Message = "StageProvider: Failed to delete stage")]
    public static partial IGenericMessage StageDeleteException(
        ILogger logger,
        Exception exception);
}
