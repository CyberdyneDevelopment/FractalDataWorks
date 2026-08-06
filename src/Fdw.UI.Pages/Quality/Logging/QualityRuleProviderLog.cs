using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Components.Logging;

/// <summary>
/// MessageLogging for QualityRuleProvider operations.
/// EventId range: 4400-4414
/// </summary>
[MessageLoggingTypeCode("COMPONENTS13")]
public static partial class QualityRuleProviderLog
{
    /// <summary>
    /// Logs that quality rules are being loaded.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "QualityRuleProvider: Loading quality rules")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    /// <summary>
    /// Logs that quality rules were loaded, including the count.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="count">The number of quality rules that were loaded.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "QualityRuleProvider: Loaded {count} quality rules")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int count);

    /// <summary>
    /// Logs that loading quality rules failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning,
        Message = "QualityRuleProvider: Failed to load quality rules")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while loading quality rules.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while loading quality rules.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning,
        Message = "QualityRuleProvider: Exception loading quality rules")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that a quality rule is being created.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the quality rule being created.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Creating quality rule '{name}'")]
    public static partial IGenericMessage Creating(ILogger logger, string name);

    /// <summary>
    /// Logs that a quality rule was created.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the quality rule that was created.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Quality rule '{name}' created")]
    public static partial IGenericMessage Created(ILogger logger, string name);

    /// <summary>
    /// Logs that creating a quality rule failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="name">The name of the quality rule that failed to create.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Failed to create quality rule '{name}'")]
    public static partial IGenericMessage CreateFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while creating a quality rule.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while creating the quality rule.</param>
    /// <param name="name">The name of the quality rule that failed to create.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Exception creating quality rule '{name}'")]
    public static partial IGenericMessage CreateException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that a quality rule is being updated.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule being updated.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Updating quality rule '{id}'")]
    public static partial IGenericMessage Updating(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a quality rule was updated.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule that was updated.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Quality rule '{id}' updated")]
    public static partial IGenericMessage Updated(ILogger logger, Guid id);

    /// <summary>
    /// Logs that updating a quality rule failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule that failed to update.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 71003, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Failed to update quality rule '{id}'")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while updating a quality rule.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while updating the quality rule.</param>
    /// <param name="id">The identifier of the quality rule that failed to update.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Exception updating quality rule '{id}'")]
    public static partial IGenericMessage UpdateException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that a quality rule is being deleted.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule being deleted.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Deleting quality rule '{id}'")]
    public static partial IGenericMessage Deleting(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a quality rule was deleted.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule that was deleted.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Quality rule '{id}' deleted")]
    public static partial IGenericMessage Deleted(ILogger logger, Guid id);

    /// <summary>
    /// Logs that deleting a quality rule failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule that failed to delete.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Failed to delete quality rule '{id}'")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while deleting a quality rule.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while deleting the quality rule.</param>
    /// <param name="id">The identifier of the quality rule that failed to delete.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Exception deleting quality rule '{id}'")]
    public static partial IGenericMessage DeleteException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that a quality check is being executed for a quality rule.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule whose check is being executed.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Executing quality check for rule '{id}'")]
    public static partial IGenericMessage Executing(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a quality check for a quality rule was executed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule whose check was executed.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "QualityRuleProvider: Quality check for rule '{id}' executed")]
    public static partial IGenericMessage Executed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that executing a quality check for a quality rule failed.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="id">The identifier of the quality rule whose check failed to execute.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Failed to execute quality check for rule '{id}'")]
    public static partial IGenericMessage ExecuteFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while executing a quality check for a quality rule.
    /// </summary>
    /// <param name="logger">The logger to write the event to.</param>
    /// <param name="exception">The exception that was thrown while executing the quality check.</param>
    /// <param name="id">The identifier of the quality rule whose check failed to execute.</param>
    /// <returns>The structured IGenericMessage for the event.</returns>
    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "QualityRuleProvider: Exception executing quality check for rule '{id}'")]
    public static partial IGenericMessage ExecuteException(ILogger logger, Exception exception, Guid id);
}
