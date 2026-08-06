using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Components.Logging;

/// <summary>
/// MessageLogging for PipelineBuilderProvider DataSet loading operations.
/// EventId range: 4288-4297
/// </summary>
[MessageLoggingTypeCode("COMPONENTS12")]
public static partial class PipelineBuilderDataSetLog
{
    /// <summary>
    /// Logs that the PipelineBuilderProvider has started loading DataSets from the API.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "PipelineBuilderProvider: Loading DataSets from API")]
    public static partial IGenericMessage LoadingDataSets(ILogger logger);

    /// <summary>
    /// Logs that the PipelineBuilderProvider loaded the given number of DataSets.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of DataSets that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "PipelineBuilderProvider: Loaded {count} DataSets")]
    public static partial IGenericMessage LoadedDataSets(ILogger logger, int count);

    /// <summary>
    /// Logs that the PipelineBuilderProvider failed to load DataSets from the API.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "PipelineBuilderProvider: Failed to load DataSets from API")]
    public static partial IGenericMessage LoadDataSetsFailed(ILogger logger);

    /// <summary>
    /// Logs that the PipelineBuilderProvider raised an exception while loading DataSets.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception raised while loading DataSets.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "PipelineBuilderProvider: Exception loading DataSets")]
    public static partial IGenericMessage LoadDataSetsException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the PipelineBuilderProvider is creating a DataSet from the inline editor.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataSet being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "PipelineBuilderProvider: Creating DataSet '{name}' from inline editor")]
    public static partial IGenericMessage CreatingDataSet(ILogger logger, string name);

    /// <summary>
    /// Logs that the PipelineBuilderProvider created a DataSet successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataSet that was created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "PipelineBuilderProvider: DataSet '{name}' created successfully")]
    public static partial IGenericMessage DataSetCreated(ILogger logger, string name);

    /// <summary>
    /// Logs that the PipelineBuilderProvider failed to create a DataSet.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the DataSet that failed to be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "PipelineBuilderProvider: Failed to create DataSet '{name}'")]
    public static partial IGenericMessage CreateDataSetFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that the PipelineBuilderProvider raised an exception while creating a DataSet.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception raised while creating the DataSet.</param>
    /// <param name="name">The name of the DataSet being created when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "PipelineBuilderProvider: Exception creating DataSet '{name}'")]
    public static partial IGenericMessage CreateDataSetException(ILogger logger, Exception exception, string name);
}
