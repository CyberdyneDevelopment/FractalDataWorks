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
/// single value, fails the loop; it is not treated as a list of one. With a batch size, a round takes that many items in order
/// (the last round may take fewer) and its variable holds them as a list; without one, a round takes one item (FDW-3086).
/// </remarks>
public sealed class FlowForEach : IFlowStep
{
    private readonly string _variable;
    private readonly string _list;
    private readonly int? _batchSize;
    private readonly IReadOnlyList<IFlowStep> _body;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowForEach"/> class.</summary>
    /// <param name="name">The step's name.</param>
    /// <param name="variable">The element's name, declared for each iteration.</param>
    /// <param name="list">The variable that holds the list.</param>
    /// <param name="body">The steps run each iteration; at least one.</param>
    /// <param name="batchSize">How many items a round takes, as a list; none for one item a round. At least one.</param>
    /// <param name="logger">The logger.</param>
    public FlowForEach(string name, string variable, string list, IReadOnlyList<IFlowStep> body, int? batchSize = null, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        ArgumentException.ThrowIfNullOrWhiteSpace(list);
        ArgumentNullException.ThrowIfNull(body);
        if (body.Count == 0)
        {
            throw new ArgumentException("A loop's body has at least one step.", nameof(body));
        }

        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "A loop's batch holds at least one item.");
        }

        Name = name;
        _variable = variable;
        _list = list;
        _batchSize = batchSize;
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

        var items = elements.Cast<object?>().ToList();
        if (_batchSize is { } size)
        {
            FlowLog.LoopBatched(_logger, Name, _list, items.Count, size);
        }

        var round = 0;
        foreach (var element in Rounds(items, _batchSize))
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

    // What each round's variable holds: the item, or with a batch size the next that many items as a list.
    private static IEnumerable<object?> Rounds(IReadOnlyList<object?> items, int? batchSize) =>
        batchSize is { } size ? items.Chunk(size).Select(batch => (object?)batch.ToList()) : items;
}
