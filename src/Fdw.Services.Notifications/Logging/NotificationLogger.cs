using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Notifications.Logging;

/// <summary>
/// Static logger class for notification service operations.
/// Uses EventIds 7501-7600.
/// </summary>
[MessageLoggingTypeCode("NOTIFICATION")]
public static partial class NotificationLogger
{
    // ========================================
    // Dispatcher Operations (7501-7520)
    // ========================================

    /// <summary>
    /// Logs when a notification is being sent.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Sending notification via channel {channel} to {recipientCount} recipients")]
    public static partial IGenericMessage SendingNotification(
        ILogger logger,
        string channel,
        int recipientCount);

    /// <summary>
    /// Logs when a notification is sent successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Notification {requestId} sent successfully via {channel}")]
    public static partial IGenericMessage NotificationSent(
        ILogger logger,
        string requestId,
        string channel);

    /// <summary>
    /// Logs when a notification fails to send.
    /// </summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Failed to send notification {requestId} via {channel}: {error}")]
    public static partial IGenericMessage NotificationFailed(
        ILogger logger,
        string requestId,
        string channel,
        string error);

    /// <summary>
    /// Logs when a channel is not found.
    /// </summary>
    [MessageLogging(
        EventId = 30000,
        Level = LogLevel.Error,
        Message = "Notification channel {channel} not found or not available")]
    public static partial IGenericMessage ChannelNotFound(
        ILogger logger,
        string channel);

    /// <summary>
    /// Logs when a batch of notifications is being sent.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Sending batch of {count} notifications")]
    public static partial IGenericMessage SendingBatch(
        ILogger logger,
        int count);

    /// <summary>
    /// Logs when a batch completes.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Batch complete: {successCount} succeeded, {failCount} failed")]
    public static partial IGenericMessage BatchComplete(
        ILogger logger,
        int successCount,
        int failCount);

    // ========================================
    // Email Operations (7521-7540)
    // ========================================

    /// <summary>
    /// Logs when connecting to SMTP server.
    /// </summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Debug,
        Message = "Connecting to SMTP server {host}:{port}")]
    public static partial IGenericMessage ConnectingToSmtp(
        ILogger logger,
        string host,
        int port);

    /// <summary>
    /// Logs when SMTP authentication is starting.
    /// </summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Debug,
        Message = "Authenticating with SMTP server")]
    public static partial IGenericMessage SmtpAuthenticating(ILogger logger);

    /// <summary>
    /// Logs when email is sent.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Debug,
        Message = "Email sent to {recipientCount} recipients")]
    public static partial IGenericMessage EmailSent(
        ILogger logger,
        int recipientCount);

    /// <summary>
    /// Logs when SMTP connection fails.
    /// </summary>
    [MessageLogging(
        EventId = 71001,
        Level = LogLevel.Error,
        Message = "Failed to connect to SMTP server: {error}")]
    public static partial IGenericMessage SmtpConnectionFailed(
        ILogger logger,
        Exception exception,
        string error);

    /// <summary>
    /// Logs when email sending fails.
    /// </summary>
    [MessageLogging(
        EventId = 71002,
        Level = LogLevel.Error,
        Message = "Failed to send email: {error}")]
    public static partial IGenericMessage EmailSendFailed(
        ILogger logger,
        Exception exception,
        string error);





    // ========================================
    // Webhook Operations (7561-7580)
    // ========================================

    /// <summary>
    /// Logs when sending generic webhook notification.
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Debug,
        Message = "Sending webhook notification to {url}")]
    public static partial IGenericMessage SendingWebhookNotification(
        ILogger logger,
        string url);

    /// <summary>
    /// Logs when webhook notification is sent.
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Webhook notification sent, received status {statusCode}")]
    public static partial IGenericMessage WebhookSent(
        ILogger logger,
        int statusCode);

    /// <summary>
    /// Logs when webhook call fails.
    /// </summary>
    [MessageLogging(
        EventId = 71005,
        Level = LogLevel.Error,
        Message = "Webhook call failed: {error}")]
    public static partial IGenericMessage WebhookFailed(
        ILogger logger,
        Exception exception,
        string error);

    // ========================================
    // Validation Operations (7581-7590)
    // ========================================

    /// <summary>
    /// Logs when request validation fails.
    /// </summary>
    [MessageLogging(
        EventId = 21003,
        Level = LogLevel.Warning,
        Message = "Notification request validation failed: {reason}")]
    public static partial IGenericMessage ValidationFailed(
        ILogger logger,
        string reason);

    /// <summary>
    /// Logs when no recipients are provided.
    /// </summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Notification request has no recipients")]
    public static partial IGenericMessage NoRecipients(ILogger logger);

    /// <summary>
    /// Logs when message is empty.
    /// </summary>
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Warning,
        Message = "Notification message cannot be empty")]
    public static partial IGenericMessage EmptyMessage(ILogger logger);

    // ========================================
    // Configuration Operations (7591-7600)
    // ========================================

    /// <summary>
    /// Logs when notification services are being registered.
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Debug,
        Message = "Registering notification services")]
    public static partial IGenericMessage RegisteringServices(ILogger logger);

    /// <summary>
    /// Logs when a notification channel is registered.
    /// </summary>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Debug,
        Message = "Registered notification channel: {channel}")]
    public static partial IGenericMessage ChannelRegistered(
        ILogger logger,
        string channel);

    /// <summary>
    /// Logs when email configuration is not valid.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "Email configuration is not valid: {reason}")]
    public static partial IGenericMessage InvalidEmailConfiguration(
        ILogger logger,
        string reason);

}
