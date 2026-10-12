using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// What a validation pass of one calculation array remembers: the results defined so far, the input fields read, and how
/// many nodes were seen.
/// </summary>
internal sealed class CalculationValidationContext : ICalculationScope
{
    private readonly HashSet<string> _defined = new(StringComparer.Ordinal);
    private readonly HashSet<string> _fields = new(StringComparer.OrdinalIgnoreCase);
    private readonly CalculationLimits _limits;
    private int _nodes;

    public CalculationValidationContext(CalculationLimits limits)
    {
        _limits = limits;
    }

    public HashSet<string> Defined => _defined;

    public bool HasField(string name) => _fields.Contains(name);

    public bool IsDefined(string result) => _defined.Contains(result);

    public void Reads(string field) => _fields.Add(field);

    public bool Define(string result) => _defined.Add(result);

    // Counts a node and refuses the array when the node count or the nesting depth passes a stated limit.
    public IGenericResult Enter(CalculationNode node, int depth)
    {
        _nodes++;
        if (_nodes > _limits.MaxNodes)
        {
            return Exceeded(node, $"the array has more than {_limits.MaxNodes} nodes");
        }

        return depth > _limits.MaxDepth
            ? Exceeded(node, $"a tree is nested deeper than {_limits.MaxDepth}")
            : GenericResult.Success();
    }

    private static IGenericResult Exceeded(CalculationNode node, string reason) =>
        GenericResult.Failure(
            CalculationTreeResultCodes.ByName("LimitExceeded"),
            ResultDetails.Create("node", node.Describe(), "reason", reason));
}
