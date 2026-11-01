using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Base class for the options of <see cref="FlowStepBuilders"/>, using the CRTP pattern.
/// </summary>
/// <remarks>
/// The settings a step states have already been checked against its kind, so a builder reads what its kind requires without
/// testing for it. An engine step refuses what it cannot work with by throwing an <see cref="ArgumentException"/> from its
/// constructor; <see cref="Build"/> turns that into a failure that names the step.
/// </remarks>
public abstract class FlowStepBuilderBase : TypeOptionBase<FlowStepBuilderBase>, IFlowStepBuilder
{
    /// <summary>Initializes a new instance of the <see cref="FlowStepBuilderBase"/> class.</summary>
    /// <param name="name">The step kind this builder builds.</param>
    protected FlowStepBuilderBase(string name) : base(name)
    {
    }

    /// <summary>
    /// Gets whether the step does something outside the run (sends a notification, runs SQL, starts another pipeline). A test run
    /// builds such a step as one that does nothing.
    /// </summary>
    public virtual bool HasOutsideEffect => false;

    /// <inheritdoc />
    public IGenericResult<IFlowStep> Build(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (HasOutsideEffect && context.Test is not null)
        {
            return GenericResult<IFlowStep>.Success(new FlowSkippedStep(component.Name, component.ComponentType, context.Logger));
        }

        try
        {
            return Create(component, body, context);
        }
        catch (ArgumentException ex)
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, ex.Message));
        }
    }

    /// <summary>Builds the engine's step.</summary>
    /// <param name="component">The stored action or loop.</param>
    /// <param name="body">The built steps of a loop's body.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The step, or the failure that says what the step's settings get wrong.</returns>
    public abstract IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context);

    /// <summary>Fails a step whose kind requires a setting it does not state.</summary>
    /// <param name="component">The stored action or loop.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The failure.</returns>
    protected static IGenericResult<IFlowStep> Missing(PipelineComponentConfiguration component, FlowBuildContext context) =>
        GenericResult<IFlowStep>.Failure(
            GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, "a setting its kind requires is not stated"));
}
