using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for ConnectionWizard component operations.
/// EventId range: 7020-7039
/// </summary>
[MessageLoggingTypeCode("COMPONENTS8")]
public static partial class ConnectionWizardLog
{
    /// <summary>Logs when the wizard advances to the next step.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "ConnectionWizard: Advanced to step '{stepName}'")]
    public static partial IGenericMessage StepAdvanced(
        ILogger logger,
        string stepName);

    /// <summary>Logs when testing a connection in the wizard fails.</summary>
    [MessageLogging(EventId = 71021, Level = LogLevel.Warning,
        Message = "ConnectionWizard: Connection test failed")]
    public static partial IGenericMessage TestFailed(
        ILogger logger);

    /// <summary>Logs when testing a connection in the wizard fails with exception.</summary>
    [MessageLogging(EventId = 71022, Level = LogLevel.Warning,
        Message = "ConnectionWizard: Connection test failed")]
    public static partial IGenericMessage TestException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when saving a connection in the wizard fails.</summary>
    [MessageLogging(EventId = 71023, Level = LogLevel.Warning,
        Message = "ConnectionWizard: Failed to save connection")]
    public static partial IGenericMessage SaveFailed(
        ILogger logger);

    /// <summary>Logs when saving a connection in the wizard fails with exception.</summary>
    [MessageLogging(EventId = 71024, Level = LogLevel.Warning,
        Message = "ConnectionWizard: Failed to save connection")]
    public static partial IGenericMessage SaveException(
        ILogger logger,
        Exception exception);

    /// <summary>Logs when the wizard completes successfully.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "ConnectionWizard: Connection '{connectionName}' created successfully")]
    public static partial IGenericMessage WizardCompleted(
        ILogger logger,
        string connectionName);
}
