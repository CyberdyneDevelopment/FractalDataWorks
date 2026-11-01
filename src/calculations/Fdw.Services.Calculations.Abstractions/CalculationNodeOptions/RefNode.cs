using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The value of a result defined earlier in the same array. A result may use only the results before it.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Ref", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RefNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefNode"/> class.
    /// </summary>
    public RefNode()
        : base("Ref", "The value of a result defined earlier in the same array.", 0, 0)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> Members { get; } = new[] { "name" };

    /// <inheritdoc />
    public override IGenericResult Check(CalculationNode node, ICalculationScope scope)
    {
        if (node.Name is null || string.IsNullOrWhiteSpace(node.Name))
        {
            return Invalid(node, "a Ref needs the name of an earlier result");
        }

        if (!scope.IsDefined(node.Name))
        {
            return GenericResult.Failure(
                CalculationTreeResultCodes.ByName("ForwardReference"),
                ResultDetails.Create("node", node.Describe(), "reason", $"'{node.Name}' is not a result defined before this one"));
        }

        return scope.TypeOf(node.Name) == typeof(decimal)
            ? GenericResult.Success()
            : Invalid(node, $"a Ref reads a number, and result '{node.Name}' is a date-time");
    }

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        emission.Node.Name is { } name
            ? emission.Result(name)
            : throw new InvalidOperationException("A Ref node without a name reached Emit; Check refuses it");
}
