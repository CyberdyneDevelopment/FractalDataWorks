using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tracking;

/// <summary>
/// Reads what a run did from its records and events (FDW-3381).
/// </summary>
/// <remarks>
/// <para>
/// A run's flow writes, into the record of each stage that moved rows, one event for each component and one for the stage's totals (<see cref="ExecutionFlowReporter"/>).
/// This reads them back: the run's totals are the stages' added together, a stage's run time is its record's, and a component's figures are its event's. A record that
/// holds no component or totals event is not a stage of a flow (a step, a round) and is left out. A figure a record does not hold is null, so a run from before the records
/// carried it answers what it does hold.
/// </para>
/// </remarks>
public static class RunTrackingReader
{
    private const int PageSize = 200;

    /// <summary>Reads what a run did.</summary>
    /// <param name="tracker">The execution tracker.</param>
    /// <param name="runId">The run's execution record.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>What the run did, or the failure when the run is not found or its records cannot be read.</returns>
    public static async Task<IGenericResult<RunTracking>> Read(IExecutionTracker tracker, Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await tracker.GetItem(runId, cancellationToken).ConfigureAwait(false);
        if (run.IsFailure || run.Value is null)
        {
            return run.ToNewResult<RunTracking>();
        }

        var items = await StageItems(tracker, runId, cancellationToken).ConfigureAwait(false);
        if (items.IsFailure || items.Value is null)
        {
            return items.ToNewResult<RunTracking>();
        }

        var stages = new List<StageTracking>();
        foreach (var item in items.Value.OrderBy(candidate => candidate.CreatedAt))
        {
            var events = await tracker.GetEvents(item.Id, cancellationToken).ConfigureAwait(false);
            if (events.IsFailure || events.Value is null)
            {
                return events.ToNewResult<RunTracking>();
            }

            if (StageOf(item, events.Value) is { } stage)
            {
                stages.Add(stage);
            }
        }

        return GenericResult<RunTracking>.Success(RunOf(run.Value, stages));
    }

    // Every stage-type record under the run, at any depth, a page at a time.
    private static async Task<IGenericResult<List<IExecutionItem>>> StageItems(IExecutionTracker tracker, Guid runId, CancellationToken cancellationToken)
    {
        var found = new List<IExecutionItem>();
        for (var page = 1; ; page++)
        {
            var listed = await tracker.ListExecutions(page, PageSize, ExecutionItemTypes.Stage, null, null, runId, cancellationToken).ConfigureAwait(false);
            if (listed.IsFailure || listed.Value is null)
            {
                return listed.ToNewResult<List<IExecutionItem>>();
            }

            found.AddRange(listed.Value.Items.Where(item => item.Id != runId));
            if (!listed.Value.HasNext)
            {
                return GenericResult<List<IExecutionItem>>.Success(found);
            }
        }
    }

    private static StageTracking? StageOf(IExecutionItem item, IReadOnlyList<IExecutionEvent> events)
    {
        var committed = CommittedBy(events);
        var components = events
            .Where(candidate => string.Equals(candidate.EventType, ExecutionFlowReporter.ComponentEvent, StringComparison.Ordinal))
            .Select(candidate => ComponentOf(candidate.Data, committed))
            .ToList();

        // Why a destination with no component event still appears: a run that was stopped or lost never writes the component events, and the rows its destination had
        // committed are the figure that run most needs (FDW-3785).
        components.AddRange(committed
            .Where(pair => !components.Any(component => string.Equals(component.Name, pair.Key, StringComparison.OrdinalIgnoreCase)))
            .Select(pair => new ComponentTracking(pair.Key, null, null, null, null, null, null, null, null, null, pair.Value.Rows, pair.Value.Emptied, pair.Value.RepeatsToSameResult))
            .ToList());
        var totals = events.FirstOrDefault(candidate => string.Equals(candidate.EventType, ExecutionFlowReporter.TotalsEvent, StringComparison.Ordinal))?.Data;
        if (components.Count == 0 && totals is null)
        {
            return null;
        }

        // Why the stage's times come from its components and not from its record: the stage records of a flow are all closed when the flow ends, so an earlier
        // stage's record would claim the time of every stage after it. A stage ran from when its first component started to when its last one ended; a stage whose
        // components hold no time has none, and says so with null.
        var started = Earliest(components.Select(component => component.StartedAt));
        var ended = Latest(components.Select(component => component.EndedAt));
        return new StageTracking(
            item.Id, item.Name, NumberOf(item.Name), item.State.Name, started, ended, Between(started, ended),
            EventValues.Number(totals, "rowsRead"), EventValues.Number(totals, "rowsWritten"), EventValues.Number(totals, "rowsRejected"), EventValues.Number(totals, "bytesMoved"),
            [.. components.OrderBy(component => component.StartedAt).ThenBy(component => component.Name, StringComparer.Ordinal)]);
    }

    private static DateTimeOffset? Earliest(IEnumerable<DateTimeOffset?> times) => times.OfType<DateTimeOffset>().ToList() is { Count: > 0 } held ? held.Min() : null;

    private static DateTimeOffset? Latest(IEnumerable<DateTimeOffset?> times) => times.OfType<DateTimeOffset>().ToList() is { Count: > 0 } held ? held.Max() : null;

    // What each destination had committed: the greatest total any of its events states (the totals only grow), and whether any of them says the target was emptied.
    private static Dictionary<string, Committed> CommittedBy(IReadOnlyList<IExecutionEvent> events)
    {
        var committed = new Dictionary<string, Committed>(StringComparer.OrdinalIgnoreCase);
        foreach (var data in events
            .Where(candidate => string.Equals(candidate.EventType, ExecutionFlowReporter.DestinationCommittedEvent, StringComparison.Ordinal))
            .Select(candidate => candidate.Data))
        {
            if (EventValues.Text(data, "destination") is not { } destination)
            {
                continue;
            }

            var stated = new Committed(
                EventValues.Number(data, "rowsCommitted"), EventValues.Flag(data, "targetEmptied"), EventValues.Flag(data, "repeatsToSameResult"));
            committed[destination] = committed.TryGetValue(destination, out var seen)
                ? new Committed(Greater(seen.Rows, stated.Rows), Either(seen.Emptied, stated.Emptied), Either(seen.RepeatsToSameResult, stated.RepeatsToSameResult))
                : stated;
        }

        return committed;
    }

    // The figures a destination's events state, as one.
    private readonly record struct Committed(long? Rows, bool? Emptied, bool? RepeatsToSameResult);

    private static long? Greater(long? left, long? right) => (left, right) switch
    {
        ({ } l, { } r) => Math.Max(l, r),
        ({ } l, null) => l,
        _ => right,
    };

    // True when either says true, false when one says false and none true, null when neither says anything.
    private static bool? Either(bool? left, bool? right) => (left, right) switch
    {
        (true, _) or (_, true) => true,
        (false, _) or (_, false) => false,
        _ => null,
    };

    private static ComponentTracking ComponentOf(IReadOnlyDictionary<string, object?>? data, IReadOnlyDictionary<string, Committed> committed)
    {
        var started = EventValues.Time(data, "startedAt");
        var ended = EventValues.Time(data, "endedAt");
        var name = EventValues.Text(data, "component") ?? string.Empty;
        var held = committed.TryGetValue(name, out var figures) ? figures : new Committed(null, null, null);
        return new ComponentTracking(
            name, started, ended, Between(started, ended), EventValues.Flag(data, "succeeded"), EventValues.Flag(data, "failed"),
            EventValues.Number(data, "rowsIn"), EventValues.Number(data, "rowsOut"), EventValues.Number(data, "bytesOut"), EventValues.Number(data, "rejected"),
            held.Rows, held.Emptied, held.RepeatsToSameResult);
    }

    private static RunTracking RunOf(IExecutionItem run, IReadOnlyList<StageTracking> stages) => new(
        run.Id, run.Name, run.State.Name, run.StartedAt, run.CompletedAt, Between(run.StartedAt, run.CompletedAt),
        Sum(stages, stage => stage.RowsRead), Sum(stages, stage => stage.RowsWritten), Sum(stages, stage => stage.RowsRejected), Sum(stages, stage => stage.BytesMoved), stages);

    // The sum of the figures the stages hold; null when none holds it, so a run with no figures says so and does not say zero.
    private static long? Sum(IReadOnlyList<StageTracking> stages, Func<StageTracking, long?> figure) =>
        stages.Select(figure).OfType<long>().ToList() is { Count: > 0 } held ? held.Sum() : null;

    private static long? Between(DateTimeOffset? start, DateTimeOffset? end) => start is { } from && end is { } to ? (long)(to - from).TotalMilliseconds : null;

    // "Stage 2" is stage 2; "No stage" is 0; any other name states no number.
    private static int? NumberOf(string name)
    {
        if (string.Equals(name, "No stage", StringComparison.Ordinal))
        {
            return 0;
        }

        return name.StartsWith("Stage ", StringComparison.Ordinal) && int.TryParse(name.AsSpan(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;
    }
}
