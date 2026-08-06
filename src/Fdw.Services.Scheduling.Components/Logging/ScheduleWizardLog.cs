using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for ScheduleWizard component operations.
/// EventId range: 7060-7069
/// </summary>
[MessageLoggingTypeCode("COMPONENTS14")]
public static partial class ScheduleWizardLog
{
    /// <summary>Logs when the wizard advances to the next step.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "ScheduleWizard: Advanced to step '{stepName}'")]
    public static partial IGenericMessage StepAdvanced(
        ILogger logger,
        string stepName);

    /// <summary>Logs when saving a Schedule in the wizard fails.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "ScheduleWizard: Failed to save Schedule")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger);

    /// <summary>Logs when saving a Schedule in the wizard fails with exception.</summary>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "ScheduleWizard: Failed to save Schedule")]
    public static partial IGenericMessage SaveException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when the wizard completes successfully.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "ScheduleWizard: Schedule '{scheduleName}' created successfully")]
    public static partial IGenericMessage WizardCompleted(
        ILogger logger,
        string scheduleName);
}
