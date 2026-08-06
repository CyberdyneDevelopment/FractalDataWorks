using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Components.Logging;

/// <summary>
/// MessageLogging for GlossaryProvider operations.
/// EventId range: 4470-4489
/// </summary>
[MessageLoggingTypeCode("COMPONENTS7")]
public static partial class GlossaryProviderLog
{
    /// <summary>
    /// Logs that the glossary provider has started loading glossary terms.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "GlossaryProvider: Loading glossary terms")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    /// <summary>
    /// Logs that the glossary provider finished loading the given number of glossary terms.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="count">The number of glossary terms that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "GlossaryProvider: Loaded {count} glossary terms")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int count);

    /// <summary>
    /// Logs that the glossary provider failed to load glossary terms.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71002, Level = LogLevel.Warning,
        Message = "GlossaryProvider: Failed to load glossary terms")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while the glossary provider was loading glossary terms.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while loading glossary terms.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71003, Level = LogLevel.Warning,
        Message = "GlossaryProvider: Exception loading glossary terms")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the glossary provider is searching glossary terms for the given query.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="query">The search query being executed against the glossary terms.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "GlossaryProvider: Searching glossary terms for '{query}'")]
    public static partial IGenericMessage Searching(ILogger logger, string query);

    /// <summary>
    /// Logs that the glossary provider is creating a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="term">The glossary term being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "GlossaryProvider: Creating glossary term '{term}'")]
    public static partial IGenericMessage Creating(ILogger logger, string term);

    /// <summary>
    /// Logs that a glossary term was created.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="term">The glossary term that was created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information,
        Message = "GlossaryProvider: Glossary term '{term}' created")]
    public static partial IGenericMessage Created(ILogger logger, string term);

    /// <summary>
    /// Logs that the glossary provider failed to create a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="term">The glossary term that could not be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71004, Level = LogLevel.Error,
        Message = "GlossaryProvider: Failed to create glossary term '{term}'")]
    public static partial IGenericMessage CreateFailed(ILogger logger, string term);

    /// <summary>
    /// Logs that an exception occurred while creating a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while creating the glossary term.</param>
    /// <param name="term">The glossary term that was being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71005, Level = LogLevel.Error,
        Message = "GlossaryProvider: Exception creating glossary term '{term}'")]
    public static partial IGenericMessage CreateException(ILogger logger, Exception exception, string term);

    /// <summary>
    /// Logs that the glossary provider is updating a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="id">The identifier of the glossary term being updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information,
        Message = "GlossaryProvider: Updating glossary term '{id}'")]
    public static partial IGenericMessage Updating(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a glossary term was updated.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="id">The identifier of the glossary term that was updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information,
        Message = "GlossaryProvider: Glossary term '{id}' updated")]
    public static partial IGenericMessage Updated(ILogger logger, Guid id);

    /// <summary>
    /// Logs that the glossary provider failed to update a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="id">The identifier of the glossary term that could not be updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71006, Level = LogLevel.Error,
        Message = "GlossaryProvider: Failed to update glossary term '{id}'")]
    public static partial IGenericMessage UpdateFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while updating a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while updating the glossary term.</param>
    /// <param name="id">The identifier of the glossary term that was being updated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71007, Level = LogLevel.Error,
        Message = "GlossaryProvider: Exception updating glossary term '{id}'")]
    public static partial IGenericMessage UpdateException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that the glossary provider is deleting a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="id">The identifier of the glossary term being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information,
        Message = "GlossaryProvider: Deleting glossary term '{id}'")]
    public static partial IGenericMessage Deleting(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a glossary term was deleted.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="id">The identifier of the glossary term that was deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11016, Level = LogLevel.Information,
        Message = "GlossaryProvider: Glossary term '{id}' deleted")]
    public static partial IGenericMessage Deleted(ILogger logger, Guid id);

    /// <summary>
    /// Logs that the glossary provider failed to delete a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="id">The identifier of the glossary term that could not be deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71008, Level = LogLevel.Error,
        Message = "GlossaryProvider: Failed to delete glossary term '{id}'")]
    public static partial IGenericMessage DeleteFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while deleting a glossary term.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="exception">The exception that was raised while deleting the glossary term.</param>
    /// <param name="id">The identifier of the glossary term that was being deleted.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71009, Level = LogLevel.Error,
        Message = "GlossaryProvider: Exception deleting glossary term '{id}'")]
    public static partial IGenericMessage DeleteException(ILogger logger, Exception exception, Guid id);
}
