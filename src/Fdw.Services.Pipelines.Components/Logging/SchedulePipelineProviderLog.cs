using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Components.Logging;

/// <summary>
/// MessageLogging for SchedulePipelineProvider operations.
/// EventId range: 4270-4279
/// </summary>
[MessageLoggingTypeCode("COMPONENTS12")]
public static partial class SchedulePipelineProviderLog
{
    /// <summary>
    /// Logs that pipelines are being loaded for schedule selection.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Trace,
        Message = "Loading pipelines for schedule selection")]
    public static partial IGenericMessage LoadingPipelines(ILogger logger);

    /// <summary>
    /// Logs that pipelines were loaded for schedule selection.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of pipelines that were loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Information,
        Message = "Loaded {count} pipelines for schedule selection")]
    public static partial IGenericMessage LoadedPipelines(ILogger logger, int count);

    /// <summary>
    /// Logs that loading pipelines for schedule selection failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "Failed to load pipelines for schedule selection")]
    public static partial IGenericMessage LoadPipelinesFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while loading pipelines for schedule selection.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while loading pipelines.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(
        EventId = 91008,
        Level = LogLevel.Error,
        Message = "Failed to load pipelines for schedule selection")]
    public static partial IGenericMessage LoadPipelinesException(ILogger logger, Exception exception);
}
