using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A for-each loop: runs its body once for each element of a list held in a variable (FDW-1403).
/// </summary>
/// <remarks>
/// The list is read when the loop starts and iterated as it was then. A variable that is not declared, or holds text or a
/// single value, fails the loop; it is not treated as a list of one.
/// </remarks>
public sealed class FlowForEach : IFlowStep
{
    private readonly string _variable;
    private readonly string _list;
    private readonly IReadOnlyList<IFlowStep> _body;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowForEach"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="variable">The element's name, declared for each iteration.</param>
    /// <param name="list">The variable that holds the list.</param>
    /// <param name="body">The steps run each iteration; at least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowForEach(string name, string variable, string list, IReadOnlyList<IFlowStep> body, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        ArgumentException.ThrowIfNullOrWhiteSpace(list);
        ArgumentNullException.ThrowIfNull(body);
        if (body.Count == 0)
        {
            throw new ArgumentException("A loop's body has at least one step.", nameof(body));
        }

        Name = name;
        _variable = variable;
        _list = list;
        _body = body;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Variables.TryGet(_list, out var held))
        {
            return GenericResult.Failure(FlowLog.VariableNotDeclared(_logger, Name, _list));
        }

        if (held is string || held is not IEnumerable elements)
        {
            return GenericResult.Failure(FlowLog.LoopNeedsList(_logger, Name, _list));
        }

        var round = 0;
        foreach (var element in elements.Cast<object?>().ToList())
        {
            var scope = context.CreateChild();
            scope.Variables.Declare(_variable, element);
            var ran = await FlowSequence.RunRound(_body, scope, ++round, cancellationToken).ConfigureAwait(false);
            if (ran.IsFailure)
            {
                return ran;
            }
        }

        return GenericResult.Success();
    }
}
