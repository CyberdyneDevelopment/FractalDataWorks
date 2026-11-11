using System;
using System.Collections.Generic;
using System.Globalization;
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
/// The handler of the <c>DisablePipeline</c> consequence (FDW-3864): records on the run whose end made the rule fire why the pipeline is disabled, and in the same save that disables
/// the pipeline marks its configuration, so the graph read can say it was the rule and not a person. A pipeline that is already disabled is left as it is. A save by a person never
/// carries the marker, so it clears it.
/// </summary>
public sealed class PipelineDisabler : IPipelineDisabler
{
    private readonly IPipelineFailureConfigurations _pipelines;
    private readonly IPipelineDisableRecord _record;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineDisabler"/> class.</summary>
    public PipelineDisabler(IPipelineFailureConfigurations pipelines, IPipelineDisableRecord record, TimeProvider time, ILogger<PipelineDisabler>? logger = null)
    {
        _pipelines = pipelines;
        _record = record;
        _time = time;
        _logger = logger ?? NullLogger<PipelineDisabler>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Disable(EscalationFiring firing, CancellationToken cancellationToken = default)
    {
        var pipeline = firing.Subject.Name;
        if (firing.Subject.RunId is not { } runId)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.DisableWithoutRun(_logger, pipeline));
        }

        var failures = FailuresOf(firing);
        if (failures is null)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.FailuresNotCarried(_logger, pipeline));
        }

        var read = await _pipelines.Read(pipeline, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.NotRead(_logger, pipeline, read.FailureReason().Message));
        }

        if (read.Value is not { IsEnabled: true } graph)
        {
            return GenericResult.Success();
        }

        var written = await _record.Write(runId, pipeline, failures.Count, firing.Reason, failures, cancellationToken).ConfigureAwait(false);
        graph.DisabledByFailureRuleAt = _time.GetUtcNow();
        graph.DisabledByFailureRuleRunId = runId;
        var disabled = await _pipelines.Disable(graph, cancellationToken).ConfigureAwait(false);
        if (disabled.IsFailure)
        {
            return GenericResult.Failure(PipelineFailureRuleLog.DisableFailed(_logger, pipeline, disabled.FailureReason().Message));
        }

        _ = PipelineFailureRuleLog.Disabled(_logger, pipeline, failures.Count, firing.RuleNumber);
        return written;
    }

    private static List<PipelineRunOutcome>? FailuresOf(EscalationFiring firing) =>
        firing.Values.TryGetValue(PipelineRunThresholdType.Failures, out var carried) && carried is string json
            ? JsonSerializer.Deserialize<List<PipelineRunOutcome>>(json)
            : null;
}
