using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reports how a pipeline's runs have been ending to the threshold evaluator after each real run (FDW-3864): the failures in a row that count, measured over the pipeline's history
/// from its last change. The thresholds attached to the pipeline say what that count means and what follows; a pipeline with none is not judged, and a pipeline that is already
/// disabled is not reported. A run in flight is left to finish: only the pipeline's configuration changes.
/// </summary>
public sealed class PipelineFailureReporter
{
    private readonly IPipelineFailureConfigurations _pipelines;
    private readonly IPipelineRunHistory _history;
    private readonly IThresholdSource _thresholds;
    private readonly IThresholdEvaluator _evaluator;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineFailureReporter"/> class.</summary>
    public PipelineFailureReporter(
        IPipelineFailureConfigurations pipelines,
        IPipelineRunHistory history,
        IThresholdSource thresholds,
        IThresholdEvaluator evaluator,
        ILogger<PipelineFailureReporter>? logger = null)
    {
        _pipelines = pipelines;
        _history = history;
        _thresholds = thresholds;
        _evaluator = evaluator;
        _logger = logger ?? NullLogger<PipelineFailureReporter>.Instance;
    }

    /// <summary>Reports the pipeline's run history once.</summary>
    /// <param name="pipelineName">The pipeline whose run ended.</param>
    /// <param name="executionId">The run that ended.</param>
    /// <param name="tenantId">The tenant the run belongs to.</param>
    /// <param name="cancellationToken">Cancels the report.</param>
    /// <returns>Success when the thresholds were judged, whatever fired; a failure that has been logged when they could not be, or when a consequence failed.</returns>
    public async Task<IGenericResult> Report(string pipelineName, Guid executionId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var read = await _pipelines.Read(pipelineName, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.NotRead(_logger, pipelineName, read.FailureReason().Message));
        }

        if (read.Value is not { IsEnabled: true } graph)
        {
            return GenericResult.Success();
        }

        if (tenantId is not { } tenant)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.NoTenant(_logger, pipelineName));
        }

        var subject = new ThresholdSubject(PipelineRunThresholdType.Subject, pipelineName, graph.Id, tenant, executionId);
        var attached = await _thresholds.For(subject, cancellationToken).ConfigureAwait(false);
        if (attached.IsFailure || attached.Value is null)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.NotJudged(_logger, pipelineName, attached.NoValueReason().Message));
        }

        // Each threshold says which outcomes it counts (FDW-4278), so the history is counted once per distinct choice and each threshold is judged with its own count.
        var counts = new Dictionary<bool, PipelineFailureCount>();
        var fired = new List<ThresholdFired>();
        foreach (var threshold in attached.Value.Where(candidate => candidate.IsEnabled && candidate.Type is PipelineRunThresholdType))
        {
            var countsTimedOut = CountsTimedOut(threshold, pipelineName);
            if (countsTimedOut.IsFailure)
            {
                return countsTimedOut;
            }

            var timedOut = countsTimedOut.Value;
            if (!counts.TryGetValue(timedOut, out var count))
            {
                var counted = await PipelineFailureHistory.Count(_history, pipelineName, graph.ModifyDate, timedOut, cancellationToken).ConfigureAwait(false);
                if (counted.IsFailure || counted.Value is null)
                {
                    return GenericResult.Failure(PipelineFailureRuleLog.NotRead(_logger, pipelineName, counted.NoValueReason().Message));
                }

                count = counts[timedOut] = counted.Value;
            }

            var judged = await _evaluator.Judge(subject, [threshold], Values(count), cancellationToken).ConfigureAwait(false);
            if (judged.IsFailure || judged.Value is null)
            {
                return GenericResult.Failure(PipelineFailureRuleLog.NotJudged(_logger, pipelineName, judged.NoValueReason().Message));
            }

            fired.AddRange(judged.Value.Fired);
        }

        LogFailedConsequences(fired, pipelineName);
        var failed = fired.Where(consequence => consequence.Result.IsFailure).ToList();
        return failed.Count == 0 ? GenericResult.Success() : failed[0].Result;
    }

    // Whether the threshold counts a run that timed out: its CountedOutcomes names TimedOut. A name that is neither Failed nor TimedOut is refused, naming it.
    private IGenericResult<bool> CountsTimedOut(IThresholdDefinition threshold, string pipelineName)
    {
        var timedOut = false;
        _ = threshold.Parameters.TryGetValue(PipelineRunThresholdType.CountedOutcomes, out var stated);
        foreach (var named in (stated as IEnumerable<object?>) ?? [])
        {
            var outcome = Convert.ToString(named, CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.Equals(outcome, PipelineRunThresholdType.TimedOutOutcome, StringComparison.Ordinal))
            {
                timedOut = true;
            }
            else if (!string.Equals(outcome, PipelineRunThresholdType.FailedOutcome, StringComparison.Ordinal))
            {
                return GenericResult<bool>.Failure(PipelineFailureRuleLog.CountedOutcomeUnknown(_logger, threshold.Name, pipelineName, outcome));
            }
        }

        return GenericResult<bool>.Success(timedOut);
    }

    // The guard sits on each result read: FDW057 cannot see a filter applied in a Where lambda.
    private void LogFailedConsequences(IReadOnlyList<ThresholdFired> fired, string pipelineName)
    {
        foreach (var consequence in fired)
        {
            if (consequence.Result.IsFailure)
            {
                _ = PipelineFailureRuleLog.ConsequenceFailed(_logger, consequence.ThresholdName, consequence.RuleNumber, pipelineName, consequence.Escalation, consequence.Result.FailureReason().Message);
            }
        }
    }

    // The newest counted failure's category is reported only when it has one; a failure that carried none reports no category, not a stand-in word.
    private static Dictionary<string, object?> Values(PipelineFailureCount count)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [PipelineRunThresholdType.ConsecutiveFailures] = count.Count,
            [PipelineRunThresholdType.Failures] = JsonSerializer.Serialize(count.Failures),
        };
        if (count.Failures is [{ Category: { } category }, ..])
        {
            values[PipelineRunThresholdType.LastFailureCategory] = category;
        }

        // The connection of the newest counted failure (FDW-4192), reported only when that failure recorded one: a DisableConnection rule acts on it.
        if (count.Failures is [{ ConnectionId: { } connectionId }, ..])
        {
            values[PipelineRunThresholdType.LastFailureConnectionId] = connectionId.ToString();
        }

        return values;
    }
}
