using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines;

/// <summary>
/// A rule notifier fake for the Notify tests (FDW-1120): a channel that records what it was asked to send, in order, in a
/// shared event list ("notify:&lt;rule&gt;:&lt;message&gt;"), and can be told a rule does not exist or that a rule's send fails.
/// </summary>
internal sealed class FakeRuleNotifier : IRuleNotifier
{
    private readonly List<string> _events;

    /// <summary>Initializes a new instance of the <see cref="FakeRuleNotifier"/> class.</summary>
    /// <param name="events">Receives one entry per notification sent, in order.</param>
    public FakeRuleNotifier(List<string> events)
    {
        _events = events;
    }

    /// <summary>Gets the rules that do not exist.</summary>
    public HashSet<string> MissingRules { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the rules whose send fails.</summary>
    public HashSet<string> FailingRules { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the notifications sent, in order.</summary>
    public List<RuleNotification> Sent { get; } = [];

    /// <inheritdoc />
    public Task<IGenericResult> Exists(string ruleName, CancellationToken cancellationToken = default) =>
        Task.FromResult(MissingRules.Contains(ruleName)
            ? GenericResult.Failure(new GenericMessage($"rule '{ruleName}' was not found"))
            : GenericResult.Success());

    /// <inheritdoc />
    public Task<IGenericResult> Notify(RuleNotification notification, CancellationToken cancellationToken = default)
    {
        _events.Add($"notify:{notification.RuleName}:{notification.Message}");
        Sent.Add(notification);
        return Task.FromResult(FailingRules.Contains(notification.RuleName)
            ? GenericResult.Failure(new GenericMessage($"channel of '{notification.RuleName}' refused"))
            : GenericResult.Success());
    }
}
