using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for DataSetWizard component operations.
/// EventId range: 7040-7049
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataSetWizardLog
{
    /// <summary>Logs when the wizard advances to the next step.</summary>
    [MessageLogging(EventId = 11032, Level = LogLevel.Trace,
        Message = "DataSetWizard: Advanced to step '{stepName}'")]
    public static partial IGenericMessage StepAdvanced(
        ILogger logger,
        string stepName);

    /// <summary>Logs when the user selects a DataSet type.</summary>
    [MessageLogging(EventId = 11033, Level = LogLevel.Trace,
        Message = "DataSetWizard: DataSet type '{typeName}' selected")]
    public static partial IGenericMessage TypeSelected(
        ILogger logger,
        string typeName);

    /// <summary>Logs when saving a DataSet in the wizard fails.</summary>
    [MessageLogging(EventId = 71033, Level = LogLevel.Warning,
        Message = "DataSetWizard: Failed to save DataSet")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger);

    /// <summary>Logs when saving a DataSet in the wizard fails with exception.</summary>
    [MessageLogging(EventId = 71034, Level = LogLevel.Warning,
        Message = "DataSetWizard: Failed to save DataSet")]
    public static partial IGenericMessage SaveException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when the wizard completes successfully.</summary>
    [MessageLogging(EventId = 11034, Level = LogLevel.Information,
        Message = "DataSetWizard: DataSet '{dataSetName}' created successfully")]
    public static partial IGenericMessage WizardCompleted(
        ILogger logger,
        string dataSetName);
}
