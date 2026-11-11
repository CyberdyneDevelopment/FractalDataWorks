using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reads how a pipeline's runs ended, newest first, for the failure rule (FDW-3864). A run that succeeded ends the count; a failed run counts unless the kind of its failure
/// (the <c>run.failed</c> event of FDW-3862) says it is transient; a failed run that recorded no kind (one from before the kind was recorded) counts as unclassified and is logged; a run that is still running, was cancelled or was interrupted is passed over; a run that timed out is read as such, and whether it counts is the threshold's to say (FDW-4278).
/// </summary>
public static class PipelineRunOutcomeReader
{
    private const string SucceededStatus = "Succeeded";
    private const string FailedStatus = "Failed";
    private const string TimedOutStatus = "TimedOut";

    private static DataStoreTarget Target(string connection) => new(connection, "etl", "PipelineExecution");

    /// <summary>Reads one page of the pipeline's runs.</summary>
    /// <param name="gateway">The gateway that reads the operational store.</param>
    /// <param name="tracker">The execution tracker whose events carry the failure kinds.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="connection">The operational connection name.</param>
    /// <param name="pipelineName">The pipeline.</param>
    /// <param name="skip">How many of the newest runs to skip.</param>
    /// <param name="take">How many runs to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The runs, newest first; empty when there are no more.</returns>
    public static async Task<IGenericResult<IReadOnlyList<PipelineRunOutcome>>> Page(
        IDataGateway gateway, IExecutionTracker tracker, ILogger logger, string connection, string pipelineName, int skip, int take, CancellationToken cancellationToken = default)
    {
        var read = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<Dictionary<string, object?>>
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition { PropertyName = "PipelineName", Operator = FilterOperators.ByName("Equal"), Value = pipelineName },
                },
                Ordering = new OrderingExpression
                {
                    OrderedFields =
                    [
                        new OrderedField { PropertyName = "StartedAt", Direction = SortDirections.ByName("Descending") },
                        new OrderedField { PropertyName = "Id", Direction = SortDirections.ByName("Descending") },
                    ],
                },
                Paging = new PagingExpression { Skip = skip, Take = take },
            },
            Target(connection), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<IReadOnlyList<PipelineRunOutcome>>();
        }

        var outcomes = new List<PipelineRunOutcome>();
        foreach (var row in read.Value)
        {
            if (Convert.ToString(row["Status"], CultureInfo.InvariantCulture) is not { } status
                || (!string.Equals(status, SucceededStatus, StringComparison.Ordinal) && !string.Equals(status, FailedStatus, StringComparison.Ordinal)
                    && !string.Equals(status, TimedOutStatus, StringComparison.Ordinal)))
            {
                continue;
            }

            var outcome = await Outcome(tracker, logger, pipelineName, row, status, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure || outcome.Value is null)
            {
                return outcome.ToNewResult<IReadOnlyList<PipelineRunOutcome>>();
            }

            outcomes.Add(outcome.Value);
        }

        return GenericResult<IReadOnlyList<PipelineRunOutcome>>.Success(outcomes);
    }

    private static async Task<IGenericResult<PipelineRunOutcome>> Outcome(
        IExecutionTracker tracker, ILogger logger, string pipelineName, Dictionary<string, object?> row, string status, CancellationToken cancellationToken)
    {
        var id = (Guid)Convert.ChangeType(row["Id"]!, typeof(Guid), CultureInfo.InvariantCulture);
        if (!row.TryGetValue("CompletedAt", out var completed) || completed is null)
        {
            return GenericResult<PipelineRunOutcome>.Failure(PipelineFailureRuleLog.RunTimeMissing(logger, id, pipelineName, status));
        }

        var at = ToOffset(completed);
        if (string.Equals(status, SucceededStatus, StringComparison.Ordinal))
        {
            return GenericResult<PipelineRunOutcome>.Success(new PipelineRunOutcome(id, at, true, false, null, null, null));
        }

        // A run that passed its time limit has no run.failed event (it did not fail): it is read as timed out, and the threshold that reads it says whether it counts (FDW-4278).
        if (string.Equals(status, TimedOutStatus, StringComparison.Ordinal))
        {
            return GenericResult<PipelineRunOutcome>.Success(new PipelineRunOutcome(id, at, false, false, null, TimedOutStatus, null, true));
        }

        var events = await tracker.GetEvents(id, cancellationToken).ConfigureAwait(false);
        if (events.IsFailure || events.Value is null)
        {
            return events.ToNewResult<PipelineRunOutcome>();
        }

        var kind = events.Value.LastOrDefault(candidate => string.Equals(candidate.EventType, RunFailureClassification.Event, StringComparison.Ordinal));
        if (kind?.Data is null)
        {
            PipelineFailureRuleLog.RunKindMissing(logger, id, pipelineName);
            return GenericResult<PipelineRunOutcome>.Success(new PipelineRunOutcome(id, at, false, true, null, null, null));
        }

        var transient = kind.Data.TryGetValue("transient", out var flag) && flag is bool isTransient && isTransient;

        // Why a refused run does not count (FDW-4005): a refusal by the state of something the run uses is not the pipeline's own fault, so it is neither counted nor a reset, as a transient one.
        var refused = kind.Data.TryGetValue("refused", out var refusal) && refusal is bool isRefused && isRefused;
        return GenericResult<PipelineRunOutcome>.Success(new PipelineRunOutcome(id, at, false, !transient && !refused, Text(kind.Data, "category"), Text(kind.Data, "code"), ConnectionOf(kind.Data)));
    }

    private static DateTimeOffset ToOffset(object? value) => value switch
    {
        DateTimeOffset offset => offset,
        DateTime time => new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)),
        _ => throw new ArgumentException($"A run time of type {value?.GetType().Name} cannot be read.", nameof(value)),
    };

    // The connection a failed run recorded; a run that recorded none, or text that is not an Id, has none.
    private static Guid? ConnectionOf(IReadOnlyDictionary<string, object?> data) =>
        Guid.TryParse(Text(data, "connectionId"), out var id) ? id : null;

    private static string? Text(IReadOnlyDictionary<string, object?> data, string key) =>
        data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
}
