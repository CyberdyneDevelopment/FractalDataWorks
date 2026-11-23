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
/// A do-while loop: runs its body, then runs it again while a condition over the variables holds (FDW-1403).
/// </summary>
/// <remarks>
/// <para>
/// The body always runs at least once; the condition is judged after each run. The iteration limit is stated and is not a
/// default: a loop whose condition is still true after that many runs fails and names the step, never runs for ever and never
/// stops quietly. The body moves on by setting variables the condition reads (a Set Variable step, or a started pipeline's result).
/// </para>
/// </remarks>
public sealed class FlowDoWhile : IFlowStep
{
    private readonly Func<FlowVariables, bool> _condition;
    private readonly bool _judgedOverRound;
    private readonly int _maximum;
    private readonly IReadOnlyList<IFlowStep> _body;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowDoWhile"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="condition">True to run the body again; judged over the variables in scope after each run.</param>
    /// <param name="maximum">The most times the body may run; at least one.</param>
    /// <param name="body">The steps run each iteration; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowDoWhile(string name, Func<FlowVariables, bool> condition, int maximum, IReadOnlyList<IFlowStep> body, ILogger? logger = null)
        : this(name, condition, false, maximum, body, logger)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowDoWhile"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="condition">True to run the body again.</param>
    /// <param name="judgedOverRound">True when the condition is judged over the scope of the round that just ran, where a decision of the body wrote what it fired (FDW-6134); false for the variables in scope.</param>
    /// <param name="maximum">The most times the body may run; at least one.</param>
    /// <param name="body">The steps run each iteration; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowDoWhile(string name, Func<FlowVariables, bool> condition, bool judgedOverRound, int maximum, IReadOnlyList<IFlowStep> body, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, 1);
        if (body.Count == 0)
        {
            throw new ArgumentException("A loop's body has at least one step.", nameof(body));
        }

        Name = name;
        _condition = condition;
        _judgedOverRound = judgedOverRound;
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
            var round = context.CreateChild();
            var ran = await FlowSequence.RunRound(_body, round, run, cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
            {
                return ran;
            }

            if (!_condition(_judgedOverRound ? round.Variables : context.Variables))
            {
                return GenericResult.Success();
            }
        }

        return GenericResult.Failure(FlowLog.LoopLimitExceeded(_logger, Name, _maximum));
    }
}
