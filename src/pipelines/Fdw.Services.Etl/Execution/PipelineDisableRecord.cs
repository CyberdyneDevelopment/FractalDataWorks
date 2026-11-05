using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>Keeps the record of a failure-rule disable as an event of the run that passed the threshold (FDW-3864).</summary>
public sealed class PipelineDisableRecord : IPipelineDisableRecord
{
    /// <summary>The event type the record is kept under.</summary>
    public const string Event = "pipeline.disabled";

    private readonly IExecutionTracker _tracker;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineDisableRecord"/> class.</summary>
    public PipelineDisableRecord(IExecutionTracker tracker, ILogger<PipelineDisableRecord>? logger = null)
    {
        _tracker = tracker;
        _logger = logger ?? NullLogger<PipelineDisableRecord>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Write(Guid runId, string pipelineName, int consecutiveFailures, string reason, IReadOnlyList<PipelineRunOutcome> failures, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["consecutiveFailures"] = consecutiveFailures,
            ["reason"] = reason,
            ["failures"] = JsonSerializer.Serialize(failures),
        };
        var recorded = await _tracker.RecordEvent(
            runId, Event, string.Create(CultureInfo.InvariantCulture, $"Pipeline '{pipelineName}' was disabled by its failure rule after {consecutiveFailures} failures in a row"), data,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return recorded.IsFailure ? GenericResult.Failure(PipelineFailureRuleLog.DisableRecordNotWritten(_logger, runId, pipelineName, recorded.FailureReason().Message)) : GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineDisableEvidence>> Read(Guid runId, CancellationToken cancellationToken = default)
    {
        var events = await _tracker.GetEvents(runId, cancellationToken).ConfigureAwait(false);
        if (events.IsFailure || events.Value is null)
        {
            return events.ToNewResult<PipelineDisableEvidence>();
        }

        var recorded = events.Value.LastOrDefault(candidate => string.Equals(candidate.EventType, Event, StringComparison.Ordinal));
        if (recorded?.Data is null
            || !recorded.Data.TryGetValue("consecutiveFailures", out var count) || count is null
            || !recorded.Data.TryGetValue("reason", out var reason) || reason is not string why
            || !recorded.Data.TryGetValue("failures", out var failures) || failures is not string json)
        {
            return GenericResult<PipelineDisableEvidence>.Failure(PipelineFailureRuleLog.DisableRecordMissing(_logger, runId));
        }

        var list = JsonSerializer.Deserialize<List<PipelineRunOutcome>>(json);
        return list is null
            ? GenericResult<PipelineDisableEvidence>.Failure(PipelineFailureRuleLog.DisableRecordMissing(_logger, runId))
            : GenericResult<PipelineDisableEvidence>.Success(new PipelineDisableEvidence(Convert.ToInt32(count, CultureInfo.InvariantCulture), why, list));
    }
}
