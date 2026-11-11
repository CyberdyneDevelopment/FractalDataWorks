using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Results;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Notifications;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Sends the notification rules of a Notify consequence (FDW-3936), through the same delivery the run notifications use. A host that has no notification service or rule
/// provider answers a failure, so the consequence fails loud.
/// </summary>
public sealed class ThresholdNotifier : IThresholdNotifier
{
    private readonly INotificationServiceProvider? _services;
    private readonly INotificationRuleConfigurationProvider? _rules;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ThresholdNotifier"/> class.</summary>
    /// <param name="services">The notification services; none when the host registered none.</param>
    /// <param name="rules">The notification rules; none when the host registered none.</param>
    /// <param name="logger">The logger.</param>
    public ThresholdNotifier(INotificationServiceProvider? services, INotificationRuleConfigurationProvider? rules, ILogger<ThresholdNotifier>? logger = null)
    {
        _services = services;
        _rules = rules;
        _logger = logger ?? NullLogger<ThresholdNotifier>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Notify(
        IReadOnlyList<string> rules, string subject, string message, IReadOnlyDictionary<string, object?> metadata, CancellationToken cancellationToken = default)
    {
        if (_services is null || _rules is null)
        {
            return Unavailable(_services is null ? nameof(INotificationServiceProvider) : nameof(INotificationRuleConfigurationProvider));
        }

        foreach (var name in rules)
        {
            var rule = await _rules.Get(name, cancellationToken).ConfigureAwait(false);
            if (rule.IsFailure || rule.Value is null)
            {
                return rule;
            }

            var delivered = await NotificationRuleDelivery.Deliver(
                _services,
                rule.Value,
                new RuleNotification { RuleName = rule.Value.Name, Subject = subject, Message = message, Metadata = metadata },
                _logger,
                cancellationToken).ConfigureAwait(false);
            if (delivered.IsFailure)
            {
                return delivered;
            }
        }

        return GenericResult.Success();
    }

    private static IGenericResult Unavailable(string service) =>
        GenericResult.Failure(OperationsResultCodes.ByName("ThresholdNotifierUnavailable"), ResultDetails.Create().With("Service", service));
}
