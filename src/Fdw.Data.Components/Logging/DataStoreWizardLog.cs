using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for DataStoreWizard component operations.
/// EventId range: 7030-7039
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataStoreWizardLog
{
    /// <summary>Logs when the wizard advances to the next step.</summary>
    [MessageLogging(EventId = 11060, Level = LogLevel.Trace,
        Message = "DataStoreWizard: Advanced to step '{stepName}'")]
    public static partial IGenericMessage StepAdvanced(
        ILogger logger,
        string stepName);

    /// <summary>Logs when the user selects a DataStore type.</summary>
    [MessageLogging(EventId = 11061, Level = LogLevel.Trace,
        Message = "DataStoreWizard: DataStore type '{typeName}' selected")]
    public static partial IGenericMessage TypeSelected(
        ILogger logger,
        string typeName);

    /// <summary>Logs when saving a DataStore in the wizard fails.</summary>
    [MessageLogging(EventId = 71062, Level = LogLevel.Warning,
        Message = "DataStoreWizard: Failed to save DataStore")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger);

    /// <summary>Logs when saving a DataStore in the wizard fails with exception.</summary>
    [MessageLogging(EventId = 71063, Level = LogLevel.Warning,
        Message = "DataStoreWizard: Failed to save DataStore")]
    public static partial IGenericMessage SaveException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when the wizard completes successfully.</summary>
    [MessageLogging(EventId = 11062, Level = LogLevel.Information,
        Message = "DataStoreWizard: DataStore '{dataStoreName}' created successfully")]
    public static partial IGenericMessage WizardCompleted(
        ILogger logger,
        string dataStoreName);
}
