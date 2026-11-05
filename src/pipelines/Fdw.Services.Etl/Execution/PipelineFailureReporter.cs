using System;
using System.Collections.Generic;
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
    private readonly IThresholdEvaluator _evaluator;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineFailureReporter"/> class.</summary>
    public PipelineFailureReporter(
        IPipelineFailureConfigurations pipelines,
        IPipelineRunHistory history,
        IThresholdEvaluator evaluator,
        ILogger<PipelineFailureReporter>? logger = null)
    {
        _pipelines = pipelines;
        _history = history;
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

        var counted = await PipelineFailureHistory.Count(_history, pipelineName, graph.ModifyDate, cancellationToken).ConfigureAwait(false);
        if (counted.IsFailure || counted.Value is null)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.NotRead(_logger, pipelineName, counted.NoValueReason().Message));
        }

        var reported = await _evaluator.Report(
            new ThresholdSubject(PipelineRunThresholdType.Subject, pipelineName, graph.Id, tenant, executionId), Values(counted.Value), cancellationToken).ConfigureAwait(false);
        if (reported.IsFailure || reported.Value is null)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.NotJudged(_logger, pipelineName, reported.NoValueReason().Message));
        }

        LogFailedConsequences(reported.Value.Fired, pipelineName);
        var failed = reported.Value.Fired.Where(fired => fired.Result.IsFailure).ToList();
        return failed.Count == 0 ? GenericResult.Success() : failed[0].Result;
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

        return values;
    }
}
