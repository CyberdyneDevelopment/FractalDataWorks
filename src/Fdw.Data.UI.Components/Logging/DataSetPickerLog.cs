using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.UI.Components.Logging;

/// <summary>
/// MessageLogging for DataSetPicker and DataSetInlineEditor operations.
/// EventId range: 4284-4299
/// </summary>
[MessageLoggingTypeCode("UICOMPONENTS")]
public static partial class DataSetPickerLog
{
    /// <summary>
    /// Logs that the picker is creating a new DataSet with the specified name.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="name">The name of the DataSet being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "DataSetPicker: creating new DataSet '{name}'")]
    public static partial IGenericMessage CreatingDataSet(ILogger logger, string name);

    /// <summary>
    /// Logs that the DataSet with the specified name was created and selected.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="name">The name of the DataSet that was created and selected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "DataSetPicker: DataSet '{name}' created and selected")]
    public static partial IGenericMessage DataSetCreated(ILogger logger, string name);

    /// <summary>
    /// Logs that creation of the DataSet with the specified name failed.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="name">The name of the DataSet that failed to be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Warning,
        Message = "DataSetPicker: failed to create DataSet '{name}'")]
    public static partial IGenericMessage CreateDataSetFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while creating the DataSet with the specified name.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="exception">The exception that occurred while creating the DataSet.</param>
    /// <param name="name">The name of the DataSet that was being created when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "DataSetPicker: exception creating DataSet '{name}'")]
    public static partial IGenericMessage CreateDataSetException(ILogger logger, Exception exception, string name);
}
