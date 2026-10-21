using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A for loop: runs its body once for each integer from a start to an end, with the counter declared in the body's scope
/// (FDW-1403).
/// </summary>
/// <remarks>
/// The start, end and step are stated; a step of zero is refused, and a step whose sign moves away from the end runs the body no
/// times rather than for ever. Each iteration runs in its own child scope, so a stage inside the body sees the counter and what
/// the loop's outer scope declared.
/// </remarks>
public sealed class FlowForLoop : IFlowStep
{
    private readonly string _variable;
    private readonly int _start;
    private readonly int _end;
    private readonly int _step;
    private readonly IReadOnlyList<IFlowStep> _body;

    /// <summary>Initializes a new instance of the <see cref="FlowForLoop"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="variable">The counter's name, declared for each iteration.</param>
    /// <param name="start">The first value.</param>
    /// <param name="end">The last value, inclusive.</param>
    /// <param name="step">The increment; not zero.</param>
    /// <param name="body">The steps run each iteration; at least one.</param>
    public FlowForLoop(string name, string variable, int start, int end, int step, IReadOnlyList<IFlowStep> body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        ArgumentNullException.ThrowIfNull(body);
        if (step == 0)
        {
            throw new ArgumentException("A for loop's step is not zero.", nameof(step));
        }

        if (body.Count == 0)
        {
            throw new ArgumentException("A loop's body has at least one step.", nameof(body));
        }

        Name = name;
        _variable = variable;
        _start = start;
        _end = end;
        _step = step;
        _body = body;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var round = 0;
        for (var counter = _start; _step > 0 ? counter <= _end : counter >= _end; counter += _step)
        {
            var scope = context.CreateChild();
            scope.Variables.Declare(_variable, counter);
            var ran = await FlowSequence.RunRound(_body, scope, ++round, cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
            {
                return ran;
            }
        }

        return GenericResult.Success();
    }
}
