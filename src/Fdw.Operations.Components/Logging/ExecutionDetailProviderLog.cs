using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Components.Logging;

/// <summary>
/// MessageLogging for ExecutionDetailProvider operations.
/// EventId range: 4210-4219
/// </summary>
[MessageLoggingTypeCode("COMPONENTS4")]
public static partial class ExecutionDetailProviderLog
{
    /// <summary>
    /// Logs that the execution with the specified id is being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="id">The identifier of the execution being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "ExecutionDetailProvider: Loading execution '{id}'")]
    public static partial IGenericMessage LoadingExecution(ILogger logger, Guid id);

    /// <summary>
    /// Logs that the execution with the specified id was loaded.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="id">The identifier of the execution that was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "ExecutionDetailProvider: Loaded execution '{id}'")]
    public static partial IGenericMessage LoadedExecution(ILogger logger, Guid id);

    /// <summary>
    /// Logs that loading the execution with the specified id failed.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="id">The identifier of the execution that failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "ExecutionDetailProvider: Failed to load execution '{id}'")]
    public static partial IGenericMessage LoadExecutionFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while loading the execution with the specified id.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="exception">The exception that occurred while loading the execution.</param>
    /// <param name="id">The identifier of the execution that was being loaded when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71003,
        Level = LogLevel.Error,
        Message = "ExecutionDetailProvider: Failed to load execution '{id}'")]
    public static partial IGenericMessage LoadExecutionException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that the children of the execution with the specified id are being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="id">The identifier of the execution whose children are being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "ExecutionDetailProvider: Loading children for execution '{id}'")]
    public static partial IGenericMessage LoadingChildren(ILogger logger, Guid id);

    /// <summary>
    /// Logs that the specified number of children were loaded for the execution with the given id.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="count">The number of children loaded for the execution.</param>
    /// <param name="id">The identifier of the execution whose children were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "ExecutionDetailProvider: Loaded {count} children for execution '{id}'")]
    public static partial IGenericMessage LoadedChildren(ILogger logger, int count, Guid id);

    /// <summary>
    /// Logs that loading the children of the execution with the specified id failed.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="id">The identifier of the execution whose children failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71004,
        Level = LogLevel.Error,
        Message = "ExecutionDetailProvider: Failed to load children for execution '{id}'")]
    public static partial IGenericMessage LoadChildrenFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while loading the children of the execution with the specified id.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="exception">The exception that occurred while loading the children.</param>
    /// <param name="id">The identifier of the execution whose children were being loaded when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "ExecutionDetailProvider: Failed to load children for execution '{id}'")]
    public static partial IGenericMessage LoadChildrenException(ILogger logger, Exception exception, Guid id);
}
