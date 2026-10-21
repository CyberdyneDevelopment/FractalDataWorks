using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A pipeline's control flow: its steps in order (FDW-1403). Each step finishes before the next starts, so every component of
/// a stage has finished before any component of the next stage begins.
/// </summary>
public sealed class FlowPipeline : IFlowStep
{
    private readonly IReadOnlyList<IFlowStep> _steps;

    /// <summary>Initializes a new instance of the <see cref="FlowPipeline"/> class.</summary>
    /// <param name="name">The pipeline's name.</param>
    /// <param name="steps">The steps, in the order they run; at least one.</param>
    public FlowPipeline(string name, IReadOnlyList<IFlowStep> steps)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(steps);
        if (steps.Count == 0)
        {
            throw new ArgumentException("A pipeline has at least one step.", nameof(steps));
        }

        Name = name;
        _steps = steps;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        var ran = await FlowSequence.Run(_steps, context, cancellationToken).ConfigureAwait(false);
        if (ran.IsFailure)
        {
            await FireFailureHooks(context, ran, cancellationToken).ConfigureAwait(false);
        }

        return ran;
    }

    // Why the hooks' own results are not returned: the run's failure is the answer. A hook that could not do its work (a notification
    // that could not be sent) has logged that itself.
    private async Task FireFailureHooks(FlowStepContext context, IGenericResult failure, CancellationToken cancellationToken)
    {
        foreach (var hook in _steps.OfType<IFlowFailureHook>())
        {
            _ = await hook.OnFailure(context, failure, cancellationToken).ConfigureAwait(false);
        }
    }
}
