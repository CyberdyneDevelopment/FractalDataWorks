using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Analytics.Components.Logging;

/// <summary>
/// MessageLogging for PromotionReviewProvider operations.
/// EventId range: 4450-4464
/// </summary>
[MessageLoggingTypeCode("COMPONENTS19")]
public static partial class PromotionReviewProviderLog
{
    /// <summary>
    /// Logs that the provider has started loading a promotion for review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion being loaded for review.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11016, Level = LogLevel.Trace,
        Message = "PromotionReviewProvider: Loading promotion '{id}' for review")]
    public static partial IGenericMessage LoadStarted(ILogger logger, Guid id);

    /// <summary>
    /// Logs that the provider finished loading a promotion for review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that was loaded for review.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11017, Level = LogLevel.Trace,
        Message = "PromotionReviewProvider: Loaded promotion '{id}' for review")]
    public static partial IGenericMessage LoadCompleted(ILogger logger, Guid id);

    /// <summary>
    /// Logs that loading a promotion for review failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that failed to load for review.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71013, Level = LogLevel.Warning,
        Message = "PromotionReviewProvider: Failed to load promotion '{id}' for review")]
    public static partial IGenericMessage LoadFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while loading a promotion for review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while loading the promotion for review.</param>
    /// <param name="id">The identifier of the promotion being loaded when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71014, Level = LogLevel.Warning,
        Message = "PromotionReviewProvider: Exception loading promotion '{id}' for review")]
    public static partial IGenericMessage LoadException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that the provider has started approving a promotion under review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion being approved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11018, Level = LogLevel.Information,
        Message = "PromotionReviewProvider: Approving promotion '{id}'")]
    public static partial IGenericMessage Approving(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a promotion under review was approved successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that was approved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11019, Level = LogLevel.Information,
        Message = "PromotionReviewProvider: Promotion '{id}' approved")]
    public static partial IGenericMessage Approved(ILogger logger, Guid id);

    /// <summary>
    /// Logs that approving a promotion under review failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that failed to be approved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71015, Level = LogLevel.Error,
        Message = "PromotionReviewProvider: Failed to approve promotion '{id}'")]
    public static partial IGenericMessage ApproveFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while approving a promotion under review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while approving the promotion.</param>
    /// <param name="id">The identifier of the promotion being approved when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "PromotionReviewProvider: Exception approving promotion '{id}'")]
    public static partial IGenericMessage ApproveException(ILogger logger, Exception exception, Guid id);

    /// <summary>
    /// Logs that the provider has started rejecting a promotion under review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion being rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11020, Level = LogLevel.Information,
        Message = "PromotionReviewProvider: Rejecting promotion '{id}'")]
    public static partial IGenericMessage Rejecting(ILogger logger, Guid id);

    /// <summary>
    /// Logs that a promotion under review was rejected successfully.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that was rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11021, Level = LogLevel.Information,
        Message = "PromotionReviewProvider: Promotion '{id}' rejected")]
    public static partial IGenericMessage Rejected(ILogger logger, Guid id);

    /// <summary>
    /// Logs that rejecting a promotion under review failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="id">The identifier of the promotion that failed to be rejected.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71016, Level = LogLevel.Error,
        Message = "PromotionReviewProvider: Failed to reject promotion '{id}'")]
    public static partial IGenericMessage RejectFailed(ILogger logger, Guid id);

    /// <summary>
    /// Logs that an exception occurred while rejecting a promotion under review.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception thrown while rejecting the promotion.</param>
    /// <param name="id">The identifier of the promotion being rejected when the exception occurred.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "PromotionReviewProvider: Exception rejecting promotion '{id}'")]
    public static partial IGenericMessage RejectException(ILogger logger, Exception exception, Guid id);
}
