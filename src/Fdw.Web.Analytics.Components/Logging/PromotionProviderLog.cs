using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Analytics.Components.Logging;

/// <summary>
/// MessageLogging for PromotionProvider operations.
/// EventId range: 4430-4444
/// </summary>
[MessageLoggingTypeCode("COMPONENTS19")]
public static partial class PromotionProviderLog
{
    /// <summary>
    /// Logs that the provider has started loading promotion requests.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "PromotionProvider: Loading promotion requests")]
    public static partial IGenericMessage LoadStarted(ILogger logger);

    /// <summary>
    /// Logs that the provider finished loading promotion requests, reporting the count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of promotion requests that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "PromotionProvider: Loaded {count} promotion requests")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, int count);

    /// <summary>
    /// Logs that loading promotion requests failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71008, Level = LogLevel.Warning,
        Message = "PromotionProvider: Failed to load promotion requests")]
    public static partial IGenericMessage LoadFailed(ILogger logger);

    /// <summary>
    /// Logs that an exception occurred while loading promotion requests.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while loading promotion requests.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71009, Level = LogLevel.Warning,
        Message = "PromotionProvider: Exception loading promotion requests")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the provider has started creating a promotion.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the promotion being created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11010, Level = LogLevel.Information,
        Message = "PromotionProvider: Creating promotion '{name}'")]
    public static partial IGenericMessage Creating(ILogger logger, string name);

    /// <summary>
    /// Logs that a promotion was created successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the promotion that was created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11011, Level = LogLevel.Information,
        Message = "PromotionProvider: Promotion '{name}' created")]
    public static partial IGenericMessage Created(ILogger logger, string name);

    /// <summary>
    /// Logs that creating a promotion failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="name">The name of the promotion that failed to be created.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71010, Level = LogLevel.Error,
        Message = "PromotionProvider: Failed to create promotion '{name}'")]
    public static partial IGenericMessage CreateFailed(ILogger logger, string name);

    /// <summary>
    /// Logs that an exception occurred while creating a promotion.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while creating the promotion.</param>
    /// <param name="name">The name of the promotion being created when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "PromotionProvider: Exception creating promotion '{name}'")]
    public static partial IGenericMessage CreateException(ILogger logger, Exception exception, string name);

    /// <summary>
    /// Logs that the provider has started approving a promotion.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion being approved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Information,
        Message = "PromotionProvider: Approving promotion '{id}'")]
    public static partial IGenericMessage Approving(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a promotion was approved successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that was approved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information,
        Message = "PromotionProvider: Promotion '{id}' approved")]
    public static partial IGenericMessage Approved(ILogger logger, Guid id);

    /// <summary>
    /// Logs that approving a promotion failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that failed to be approved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71011, Level = LogLevel.Error,
        Message = "PromotionProvider: Failed to approve promotion '{id}'")]
    public static partial IGenericMessage ApproveFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while approving a promotion.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while approving the promotion.</param>
    /// <param name="id">The identifier of the promotion being approved when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "PromotionProvider: Exception approving promotion '{id}'")]
    public static partial IGenericMessage ApproveException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that the provider has started rejecting a promotion.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion being rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11014, Level = LogLevel.Information,
        Message = "PromotionProvider: Rejecting promotion '{id}'")]
    public static partial IGenericMessage Rejecting(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a promotion was rejected successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that was rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11015, Level = LogLevel.Information,
        Message = "PromotionProvider: Promotion '{id}' rejected")]
    public static partial IGenericMessage Rejected(ILogger logger, Guid id);

    /// <summary>
    /// Logs that rejecting a promotion failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that failed to be rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "PromotionProvider: Failed to reject promotion '{id}'")]
    public static partial IGenericMessage RejectFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while rejecting a promotion.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while rejecting the promotion.</param>
    /// <param name="id">The identifier of the promotion being rejected when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "PromotionProvider: Exception rejecting promotion '{id}'")]
    public static partial IGenericMessage RejectException(ILogger logger, Exception exception, Guid id);
}
