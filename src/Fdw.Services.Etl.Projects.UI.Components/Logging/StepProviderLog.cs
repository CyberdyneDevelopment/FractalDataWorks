using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Etl.Projects.UI.Components.Logging;

/// <summary>
/// MessageLogging methods for StepProvider operations.
/// EventId range: 8820-8827
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS2")]
public static partial class StepProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Steps (8820-8821)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the steps list fails.</summary>
    [MessageLogging(EventId = 71030, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to load steps for stage '{stageId}'")]
    public static partial IGenericMessage LoadStepsFailed(
        ILogger logger,
        string stageId);

    /// <summary>Logs when loading the steps list fails with exception.</summary>
    [MessageLogging(EventId = 71031, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to load steps list")]
    public static partial IGenericMessage LoadStepsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Step (8822-8823)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a step fails.</summary>
    [MessageLogging(EventId = 71032, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to create step")]
    public static partial IGenericMessage StepCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a step fails with exception.</summary>
    [MessageLogging(EventId = 71033, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to create step")]
    public static partial IGenericMessage StepCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Step (8824-8825)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a step fails.</summary>
    [MessageLogging(EventId = 71034, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to update step '{stepId}'")]
    public static partial IGenericMessage StepUpdateFailed(
        ILogger logger,
        string stepId);

    /// <summary>Logs when updating a step fails with exception.</summary>
    [MessageLogging(EventId = 71035, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to update step")]
    public static partial IGenericMessage StepUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Step (8826-8827)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a step fails.</summary>
    [MessageLogging(EventId = 71036, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to delete step '{stepId}'")]
    public static partial IGenericMessage StepDeleteFailed(
        ILogger logger,
        string stepId);

    /// <summary>Logs when deleting a step fails with exception.</summary>
    [MessageLogging(EventId = 71037, Level = LogLevel.Warning,
        Message = "StepProvider: Failed to delete step")]
    public static partial IGenericMessage StepDeleteException(
        ILogger logger,
        Exception exception);
}
