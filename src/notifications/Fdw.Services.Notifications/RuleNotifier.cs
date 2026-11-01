using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Logging;
using Fdw.Services.Notifications.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Notifications;

/// <summary>
/// Sends a notification through a named notification rule: reads the rule, lets the rule's own conditions decide
/// whether it sends, then delivers through the rule's notification service.
/// </summary>
public sealed class RuleNotifier : IRuleNotifier
{
    private readonly INotificationRuleConfigurationProvider _rules;
    private readonly INotificationServiceProvider _services;
    private readonly ILogger<RuleNotifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleNotifier"/> class.
    /// </summary>
    /// <param name="rules">Supplies the configured notification rules.</param>
    /// <param name="services">Resolves a rule's notification service by name.</param>
    /// <param name="logger">The logger instance.</param>
    public RuleNotifier(
        INotificationRuleConfigurationProvider rules,
        INotificationServiceProvider services,
        ILogger<RuleNotifier>? logger = null)
    {
        _rules = rules;
        _services = services;
        _logger = logger ?? NullLogger<RuleNotifier>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Exists(string ruleName, CancellationToken cancellationToken = default)
    {
        var found = await _rules.Get(ruleName, cancellationToken).ConfigureAwait(false);
        return found.IsSuccess && found.Value is not null ? GenericResult.Success() : NotFound(ruleName, found);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Notify(RuleNotification notification, CancellationToken cancellationToken = default)
    {
        var found = await _rules.Get(notification.RuleName, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess || found.Value is null)
        {
            return NotFound(notification.RuleName, found);
        }

        var rule = found.Value;
        if (!rule.IsEnabled)
        {
            RuleNotificationLog.RuleDeclined(_logger, rule.Name, "the rule is disabled");
            return GenericResult.Success();
        }

        if (!NotificationRuleScope.Covers(rule, notification.WorkflowId, notification.PipelineId, notification.ScheduleId))
        {
            RuleNotificationLog.RuleDeclined(_logger, rule.Name, "the rule's scope does not cover this notification");
            return GenericResult.Success();
        }

        return await NotificationRuleDelivery.Deliver(_services, rule, notification, _logger, cancellationToken).ConfigureAwait(false);
    }

    private IGenericResult NotFound(string ruleName, IGenericResult found)
    {
        RuleNotificationLog.RuleReadFailed(_logger, ruleName, found.CurrentMessage);
        return GenericResult.Failure(
            NotificationResultCodes.ByName("RuleNotFound"),
            ResultDetails.Create().With("RuleName", ruleName));
    }
}
