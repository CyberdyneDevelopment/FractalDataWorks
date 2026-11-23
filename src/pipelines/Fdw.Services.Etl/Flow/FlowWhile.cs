using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A while loop (FDW-6134): judges its test, runs its body, and judges the test again, so the body may run no time. The most times it runs is stated and is not a default: a loop whose
/// test still holds after that many runs fails and names the step.
/// </summary>
/// <remarks>
/// A test that is a decision's output has its decisions run first, in a scope of their own, before each judgment (<see cref="FlowLoopTest"/>); a test that is a condition text is judged
/// over the variables in scope.
/// </remarks>
public sealed class FlowWhile : IFlowStep
{
    private readonly Func<FlowVariables, bool> _test;
    private readonly IFlowStep? _decisions;
    private readonly int _maximum;
    private readonly IReadOnlyList<IFlowStep> _body;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowWhile"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="test">True to run the body; judged over the variables in scope, or over the scope the decisions ran in.</param>
    /// <param name="decisions">The decisions the test depends on, run before each judgment; <see langword="null"/> for a test that is a condition text.</param>
    /// <param name="maximum">The most times the body may run; at least one.</param>
    /// <param name="body">The steps run each iteration; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowWhile(string name, Func<FlowVariables, bool> test, IFlowStep? decisions, int maximum, IReadOnlyList<IFlowStep> body, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, 1);
        if (body.Count == 0)
        {
            throw new ArgumentException("A loop's body has at least one step.", nameof(body));
        }

        Name = name;
        _test = test;
        _decisions = decisions;
        _maximum = maximum;
        _body = body;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        for (var run = 1; run <= _maximum; run++)
        {
            var judged = await Holds(context, cancellationToken).ConfigureAwait(false);
            if (judged.IsFailure)
            {
                return judged;
            }

            if (!judged.Value)
            {
                return GenericResult.Success();
            }

            var ran = await FlowSequence.RunRound(_body, context.CreateChild(), run, cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
            {
                return ran;
            }
        }

        var last = await Holds(context, cancellationToken).ConfigureAwait(false);
        if (last.IsFailure)
        {
            return last;
        }

        return last.Value ? GenericResult.Failure(FlowLog.LoopLimitExceeded(_logger, Name, _maximum)) : GenericResult.Success();
    }

    // The test as it stands now: a condition text over the variables in scope, or the output of the decisions run ahead in a scope of their own.
    private async Task<IGenericResult<bool>> Holds(FlowStepContext context, CancellationToken cancellationToken)
    {
        if (_decisions is null)
        {
            return GenericResult<bool>.Success(_test(context.Variables));
        }

        var scope = context.CreateChild();
        var ran = await _decisions.Run(scope, cancellationToken).ConfigureAwait(false);
        return ran.IsFailure ? ran.ToNewResult<bool>() : GenericResult<bool>.Success(_test(scope.Variables));
    }
}
