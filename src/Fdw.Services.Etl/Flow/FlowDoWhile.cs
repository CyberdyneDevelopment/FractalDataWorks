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
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximum, 1);
        ArgumentNullException.ThrowIfNull(body);
        if (body.Count == 0)
        {
            throw new ArgumentException("A loop's body has at least one step.", nameof(body));
        }

        Name = name;
        _condition = condition;
        _maximum = maximum;
        _body = body;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        for (var run = 1; run <= _maximum; run++)
        {
            var ran = await FlowSequence.RunRound(_body, context.CreateChild(), run, cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
            {
                return ran;
            }

            if (!_condition(context.Variables))
            {
                return GenericResult.Success();
            }
        }

        return GenericResult.Failure(FlowLog.LoopLimitExceeded(_logger, Name, _maximum));
    }
}
