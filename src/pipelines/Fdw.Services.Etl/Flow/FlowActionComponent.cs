using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// An action or a loop (set a variable, run SQL, start another pipeline, send a notification, repeat) as a component of a flow graph (FDW-2041).
/// It has no nodes and moves no rows; it waits for its stage like any other component and then runs its step against the variables in scope.
/// </summary>
/// <remarks>
/// The step is run and recorded as a stage of the run, exactly as a step of a sequence is, so the run's record shows it under the level that holds it.
/// </remarks>
public sealed class FlowActionComponent : IFlowComponent, IFlowStepBound, IFlowStageBound
{
    private FlowStepContext? _context;
    private readonly IReadOnlyList<FlowSignal> _signals;
    private readonly ILogger _logger;
    private FlowStageRecords? _records;
    private int _stage;

    /// <summary>Initializes a new instance of the <see cref="FlowActionComponent"/> class, fed by no decision.</summary>
    /// <param name="step">The step the component runs.</param>
    public FlowActionComponent(IFlowStep step)
        : this(step, [], NullLogger.Instance)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowActionComponent"/> class.</summary>
    /// <param name="step">The step the component runs.</param>
    /// <param name="signals">The outputs of decisions whose lines lead to the component (FDW-6134): with any, the step runs only when one of them fired.</param>
    /// <param name="logger">The logger a skip is written to.</param>
    public FlowActionComponent(IFlowStep step, IReadOnlyList<FlowSignal> signals, ILogger logger)
    {
        Step = step;
        _signals = signals;
        _logger = logger;
    }

    /// <summary>Gets the step the component runs.</summary>
    public IFlowStep Step { get; }

    /// <inheritdoc />
    public string Name => Step.Name;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [];

    /// <inheritdoc />
    void IFlowStepBound.Bind(FlowStepContext context) => _context = context;

    /// <inheritdoc />
    void IFlowStageBound.BindStages(FlowStageRecords records, int stage)
    {
        _records = records;
        _stage = stage;
    }

    /// <inheritdoc />
    /// <remarks>Recorded under the record of its stage when the level records its stages (FDW-2060), and under the level's own parent otherwise.</remarks>
    public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var context = _context ?? throw new InvalidOperationException($"The action '{Name}' was run before the step it belongs to gave it a context.");

        // Why a gate is not skipped (FDW-6134): it reads its lines as values, a line that did not fire being false, so it answers whatever its lines did.
        if (_signals.Count > 0 && Step is not IFlowSignalReader && !_signals.Any(signal => FlowFired.Has(context.Variables, signal)))
        {
            return SkippedAndSaid(context, cancellationToken);
        }

        return FlowSequence.RunReported(Step, _records?.RecordOf(_stage) is { } record ? context.WithReportParent(record) : context, cancellationToken);
    }

    private async Task<IGenericResult> SkippedAndSaid(FlowStepContext context, CancellationToken cancellationToken)
    {
        var skipped = Skipped(context);
        var said = await FlowSequence.Piece(context, RunStreamKinds.StepSkipped, Name, null, cancellationToken).ConfigureAwait(false);
        return said.IsFailure ? said : skipped;
    }

    // None of the lines that lead here fired: the step does not run, and what it would have fired is false, so a component that waits on it is skipped in its turn.
    private IGenericResult Skipped(FlowStepContext context)
    {
        if (Step is IFlowDecision decision)
        {
            foreach (var output in decision.Outputs)
            {
                FlowFired.Set(context.Variables, Name, output, false);
            }
        }

        _ = FlowLog.ComponentSkippedNoSignal(_logger, Name);
        return GenericResult.Success();
    }
}
