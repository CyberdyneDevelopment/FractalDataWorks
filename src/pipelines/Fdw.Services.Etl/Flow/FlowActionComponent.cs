using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

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
    private FlowStageRecords? _records;
    private int _stage;

    /// <summary>Initializes a new instance of the <see cref="FlowActionComponent"/> class.</summary>
    /// <param name="step">The step the component runs.</param>
    public FlowActionComponent(IFlowStep step)
    {
        Step = step ?? throw new ArgumentNullException(nameof(step));
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
    void IFlowStepBound.Bind(FlowStepContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    void IFlowStageBound.BindStages(FlowStageRecords records, int stage)
    {
        _records = records ?? throw new ArgumentNullException(nameof(records));
        _stage = stage;
    }

    /// <inheritdoc />
    /// <remarks>Recorded under the record of its stage when the level records its stages (FDW-2060), and under the level's own parent otherwise.</remarks>
    public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var context = _context ?? throw new InvalidOperationException($"The action '{Name}' was run before the step it belongs to gave it a context.");
        return FlowSequence.RunReported(Step, _records?.RecordOf(_stage) is { } record ? context.WithReportParent(record) : context, cancellationToken);
    }
}
