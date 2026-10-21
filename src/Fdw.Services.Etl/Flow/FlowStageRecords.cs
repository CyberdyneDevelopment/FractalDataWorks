using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The records of the stages of one level of a pipeline run (FDW-2060): one record for each stage number that holds a component, opened when the
/// stage starts and finished with the events of its components. The components that have no stage are the stage "No stage", which runs first.
/// </summary>
/// <remarks>
/// A record is named only by what it is: the number of the stage. An action or a loop in the stage is recorded under its stage's record, and
/// the level's records are children of the step the level runs in (the run itself, or a round of a loop).
/// </remarks>
internal sealed class FlowStageRecords : IFlowStageObserver
{
    private readonly IFlowReporter _reporter;
    private readonly Guid? _parent;
    private readonly ConcurrentDictionary<int, Guid> _records = new();

    /// <summary>Initializes a new instance of the <see cref="FlowStageRecords"/> class.</summary>
    /// <param name="reporter">Writes the records.</param>
    /// <param name="parent">The record the stage records are children of; none for the run itself.</param>
    public FlowStageRecords(IFlowReporter reporter, Guid? parent)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        _parent = parent;
    }

    /// <summary>Names a stage by its number.</summary>
    /// <param name="stage">The stage number; 0 for no stage.</param>
    /// <returns>"Stage 1", "Stage 2" ...; "No stage" for 0.</returns>
    public static string NameOf(int stage) => stage == 0 ? "No stage" : $"Stage {stage}";

    /// <inheritdoc />
    public async Task StageStarting(int stage, CancellationToken cancellationToken = default)
    {
        if (await _reporter.StepStarting(NameOf(stage), _parent, cancellationToken).ConfigureAwait(false) is { } record)
        {
            _records[stage] = record;
        }
    }

    /// <summary>Gets the record of a stage that has started.</summary>
    /// <param name="stage">The stage number.</param>
    /// <returns>The record's identifier; none when the stage has not started or its record could not be written.</returns>
    public Guid? RecordOf(int stage) => _records.TryGetValue(stage, out var record) ? record : null;

    /// <summary>Writes the events of each started stage's components and finishes its record.</summary>
    /// <param name="report">What the level's flow did, for all its stages.</param>
    /// <param name="outcome">How the level's flow ended.</param>
    /// <param name="cancellationToken">Cancels the writes.</param>
    /// <returns>A task that completes when every record is finished.</returns>
    public async Task Finish(FlowRunReport report, IGenericResult outcome, CancellationToken cancellationToken)
    {
        foreach (var (stage, record) in _records.OrderBy(pair => pair.Key))
        {
            var components = report.Components.Where(component => component.Stage == stage).ToList();
            var names = components.Select(component => component.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var part = new FlowRunReport(
                [.. report.Links.Where(link => names.Contains(link.Link.FromComponent))],
                report.Elapsed,
                components,
                report.FailedComponent is { } failed && names.Contains(failed) ? failed : null,
                report.Rejections is { } rejections ? [.. rejections.Where(rejection => names.Contains(rejection.Component))] : null);
            await _reporter.FlowFinished(record, part, cancellationToken).ConfigureAwait(false);

            // Why failed only where a component of the stage did not succeed: a stage that finished before the failure is complete.
            await _reporter.StepFinished(record, outcome.IsFailure && components.Any(component => !component.Succeeded) ? outcome : GenericResult.Success(), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
