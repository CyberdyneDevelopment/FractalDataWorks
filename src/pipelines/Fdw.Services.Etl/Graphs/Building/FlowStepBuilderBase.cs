using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Logging;

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
    public virtual IReadOnlyList<Type> Needs => [];

    /// <inheritdoc />
    public IGenericResult<IFlowStep> Build(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        // Why before the test run's skip: a host that lacks what a component type's builder needs is miswired, and a test run is refused for it as a real run is.
        var needs = context.NeedsMet(component, Needs);
        if (needs.IsFailure)
        {
            return needs.ToNewResult<IFlowStep>();
        }

        if (HasOutsideEffect && context.Test is not null)
        {
            return GenericResult<IFlowStep>.Success(new FlowSkippedStep(component.Name, component.ComponentType, context.Logger));
        }

        try
        {
            var created = Create(component, body, context);
            return created.IsSuccess && created.Value is { } step && HasOutsideEffect ? Claimed(component, step, context) : created;
        }
        catch (ArgumentException ex)
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, ex.Message));
        }
    }

    // Why here, for every kind that states an outside effect (FDW-6139): one place makes each such step claim itself before it acts, so a kind added later cannot forget to. A host that registers no claim store
    // refuses the graph at build: no claim is assumed.
    private static IGenericResult<IFlowStep> Claimed(PipelineComponentConfiguration component, IFlowStep step, FlowBuildContext context)
    {
        if (context.StepClaims is not { } claims)
        {
            return GenericResult<IFlowStep>.Failure(PipelineStepClaimLog.CannotClaim(context.Logger, component.Name, "the host registers no store for the claims of steps"));
        }

        return GenericResult<IFlowStep>.Success(step is IFlowFailureHook hook
            ? new FlowClaimedFailureHookStep(hook, claims, context.Logger)
            : new FlowClaimedStep(step, claims, context.Logger));
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
