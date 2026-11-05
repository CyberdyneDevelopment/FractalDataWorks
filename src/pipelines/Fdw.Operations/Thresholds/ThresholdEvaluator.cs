using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// The one evaluator of thresholds (FDW-3936): loads the thresholds attached to a subject, or takes a set the caller holds, judges each rule of each enabled threshold against
/// the reported values with the predicate logic the data layer already has, and fires the consequence of every rule that matches.
/// </summary>
/// <remarks>
/// Nothing is assumed. A subject with no threshold is not judged. A rule that names a value the subject did not report is not judged and is reported as not measured, so a
/// missing figure never reads as zero. A consequence that fails does not stop the rules after it; its failure is in the report and in the log. A threshold that fires on entry
/// fires when its criteria start to hold and not again until they have stopped holding.
/// </remarks>
public sealed class ThresholdEvaluator : IThresholdEvaluator
{
    private readonly IThresholdSource _source;
    private readonly IServiceProvider _services;
    private readonly ThresholdEntryTracker _entries;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ThresholdEvaluator"/> class.</summary>
    /// <param name="source">Finds the thresholds attached to a subject.</param>
    /// <param name="services">The host services the consequences act through.</param>
    /// <param name="entries">Remembers which rules are holding, for thresholds that fire on entry.</param>
    /// <param name="logger">The logger.</param>
    public ThresholdEvaluator(IThresholdSource source, IServiceProvider services, ThresholdEntryTracker entries, ILogger<ThresholdEvaluator>? logger = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
        _logger = logger ?? NullLogger<ThresholdEvaluator>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ThresholdReport>> Report(ThresholdSubject subject, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        var attached = await _source.For(subject, cancellationToken).ConfigureAwait(false);
        if (attached.IsFailure || attached.Value is null)
        {
            return GenericResult<ThresholdReport>.Failure(ThresholdLog.ThresholdsNotRead(_logger, subject.Kind, subject.Name, attached.CurrentMessage));
        }

        return await Judge(subject, attached.Value, values, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ThresholdReport>> Judge(
        ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(thresholds);
        ArgumentNullException.ThrowIfNull(values);
        var enabled = thresholds.Where(threshold => threshold.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            ThresholdLog.NoThreshold(_logger, subject.Kind, subject.Name);
            return GenericResult<ThresholdReport>.Success(new ThresholdReport(0, [], []));
        }

        var named = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        var fired = new List<ThresholdFired>();
        var notMeasured = new List<ThresholdNotMeasured>();
        foreach (var threshold in enabled)
        {
            for (var index = 0; index < threshold.Rules.Count; index++)
            {
                await Judge(subject, threshold, index + 1, named, fired, notMeasured, cancellationToken).ConfigureAwait(false);
            }
        }

        ThresholdLog.Judged(_logger, subject.Kind, subject.Name, enabled.Count, fired.Count, notMeasured.Count);
        return GenericResult<ThresholdReport>.Success(new ThresholdReport(enabled.Count, fired, notMeasured));
    }

    /// <inheritdoc />
    public IReadOnlyList<ThresholdMatch> Holds(
        ThresholdSubject subject, IReadOnlyList<IThresholdDefinition> thresholds, IReadOnlyDictionary<string, object?> values, string escalation)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(thresholds);
        ArgumentNullException.ThrowIfNull(values);
        var named = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        var held = new List<ThresholdMatch>();
        foreach (var threshold in thresholds.Where(threshold => threshold.IsEnabled))
        {
            for (var index = 0; index < threshold.Rules.Count; index++)
            {
                var rule = threshold.Rules[index];
                if (string.Equals(rule.Escalation, escalation, StringComparison.Ordinal) && Matches(rule, named))
                {
                    held.Add(new ThresholdMatch(threshold.Name, index + 1, rule.Escalation, Reason(subject, threshold.Name, index + 1, rule, named)));
                }
            }
        }

        return held;
    }

    private static bool Matches(ThresholdRule rule, Dictionary<string, object?> values) =>
        ThresholdCriteria.FieldsNamed(rule.Criteria).All(values.ContainsKey) && InMemoryQuery.Matches(values, rule.Criteria);

    private static string Reason(ThresholdSubject subject, string threshold, int number, ThresholdRule rule, Dictionary<string, object?> values) =>
        $"Rule {number} of threshold '{threshold}' matched on {subject.Kind} '{subject.Name}': "
        + string.Join(", ", ThresholdCriteria.FieldsNamed(rule.Criteria).Select(field => $"{field} = {values[field]}"));

    private async Task Judge(
        ThresholdSubject subject, IThresholdDefinition threshold, int number, Dictionary<string, object?> values,
        List<ThresholdFired> fired, List<ThresholdNotMeasured> notMeasured, CancellationToken cancellationToken)
    {
        var rule = threshold.Rules[number - 1];
        var missing = ThresholdCriteria.FieldsNamed(rule.Criteria).Where(field => !values.ContainsKey(field)).ToList();
        if (missing.Count > 0)
        {
            notMeasured.Add(new ThresholdNotMeasured(threshold.Name, number, missing));
            ThresholdLog.RuleNotMeasured(_logger, threshold.Name, number, subject.Kind, subject.Name, string.Join(", ", missing));
            return;
        }

        var key = ThresholdEntryTracker.KeyOf(subject.TenantId, subject.Kind, subject.SubjectId, threshold.Name, number);
        if (!InMemoryQuery.Matches(values, rule.Criteria))
        {
            _entries.Left(key);
            return;
        }

        if (threshold.FiresOnEntry && !_entries.Entered(key))
        {
            return;
        }

        await Fire(subject, threshold, number, rule, values, fired, cancellationToken).ConfigureAwait(false);
    }

    private async Task Fire(
        ThresholdSubject subject, IThresholdDefinition threshold, int number, ThresholdRule rule, Dictionary<string, object?> values,
        List<ThresholdFired> fired, CancellationToken cancellationToken)
    {
        var reason = Reason(subject, threshold.Name, number, rule, values);
        var escalation = EscalationTypes.ByName(rule.Escalation);
        if (ReferenceEquals(escalation, EscalationTypes.NotFound))
        {
            fired.Add(new ThresholdFired(threshold.Name, number, rule.Escalation, reason, GenericResult.Failure(ThresholdLog.EscalationUnknown(_logger, threshold.Name, number, rule.Escalation))));
            return;
        }

        ThresholdLog.RuleMatched(_logger, threshold.Name, number, rule.Escalation, reason);
        var result = await escalation
            .Handle(new EscalationFiring(subject, threshold.Name, number, rule, values, reason), _services, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            ThresholdLog.ConsequenceFailed(_logger, rule.Escalation, number, threshold.Name, result.CurrentMessage);
        }

        fired.Add(new ThresholdFired(threshold.Name, number, rule.Escalation, reason, result));
    }
}
