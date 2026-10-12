using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Notifications;

/// <summary>
/// Sends one notification through one notification rule: the rule's severity becomes the priority, the rule's
/// notification service is resolved by name, and the request goes to that service. Shared by every sender that
/// notifies through a rule.
/// </summary>
public static class NotificationRuleDelivery
{
    /// <summary>
    /// The rule severity vocabulary (log levels) mapped to the delivery-urgency vocabulary of
    /// <see cref="NotificationPriorities"/>, so a rule left at the default "Info" still sends.
    /// </summary>
    private static readonly Dictionary<string, string> PriorityBySeverity =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Low"] = "Low",
            ["Info"] = "Normal",
            ["Information"] = "Normal",
            ["Normal"] = "Normal",
            ["Warning"] = "High",
            ["Warn"] = "High",
            ["High"] = "High",
            ["Error"] = "Critical",
            ["Critical"] = "Critical",
            ["Fatal"] = "Critical",
            ["Severe"] = "Critical",
        };

    /// <summary>
    /// Sends the notification through the rule.
    /// </summary>
    /// <param name="provider">Resolves the rule's notification service by name.</param>
    /// <param name="rule">The notification rule to send through.</param>
    /// <param name="notification">The notification to send.</param>
    /// <param name="logger">The logger the failures write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the service accepted the notification; otherwise a failure naming what was unusable.</returns>
    public static async Task<IGenericResult> Deliver(
        INotificationServiceProvider provider,
        INotificationRuleImplementationConfiguration rule,
        RuleNotification notification,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!PriorityBySeverity.TryGetValue(rule.Severity, out var priorityName))
        {
            return GenericResult.Failure(
                NotificationResultCodes.ByName("RuleSeverityInvalid"),
                logger,
                ResultDetails.Create().With("RuleName", rule.Name).With("Severity", rule.Severity));
        }

        var serviceResult = await provider.Get(rule.NotificationServiceName, cancellationToken).ConfigureAwait(false);
        if (!serviceResult.IsSuccess || serviceResult.Value is not INotificationService service)
        {
            RuleNotificationLog.RuleChannelUnresolved(logger, rule.Name, rule.NotificationServiceName);
            return GenericResult.Failure(
                NotificationResultCodes.ByName("ChannelNotFound"),
                ResultDetails.Create().With("ChannelName", rule.NotificationServiceName));
        }

        var sendResult = await service.Send(
            new NotificationRequest(
                channelName: rule.NotificationServiceType,
                recipients: [],
                subject: notification.Subject,
                message: notification.Message,
                priority: NotificationPriorities.ByName(priorityName),
                metadata: notification.Metadata,
                correlationId: notification.CorrelationId),
            cancellationToken).ConfigureAwait(false);
        if (sendResult.IsFailure)
        {
            RuleNotificationLog.RuleNotificationSendFailed(logger, rule.Name, rule.NotificationServiceName, sendResult.CurrentMessage);
            return GenericResult.Failure(
                NotificationResultCodes.ByName("SendFailed"),
                ResultDetails.Create().With("Message", sendResult.CurrentMessage));
        }

        RuleNotificationLog.RuleNotificationSent(logger, rule.Name, rule.NotificationServiceName);
        return GenericResult.Success();
    }
}
