using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// Rounds a number to a stated number of decimal places in a stated midpoint mode. The mode has no default: ToEven,
/// AwayFromZero, ToZero, ToNegativeInfinity or ToPositiveInfinity.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Round", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RoundNode : CalculationNodeBase
{
    private const int MaxPlaces = 28;

    private static readonly string[] ModeNames = Enum.GetNames(typeof(MidpointRounding));

    private static readonly MethodInfo Method = typeof(Math).GetMethod(nameof(Math.Round), new[] { typeof(decimal), typeof(int), typeof(MidpointRounding) })
        ?? throw new InvalidOperationException("Math.Round(decimal, int, MidpointRounding) was not found");

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundNode"/> class.
    /// </summary>
    public RoundNode()
        : base("Round", "Rounds a number to a stated number of decimal places in a stated midpoint mode.", 1, 1)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> Members { get; } = new[] { "places", "mode" };

    /// <inheritdoc />
    public override IReadOnlyList<string> Modes => ModeNames;

    /// <inheritdoc />
    public override IGenericResult Check(CalculationNode node, ICalculationScope scope)
    {
        if (node.Places is not { } places || places < 0 || places > MaxPlaces)
        {
            return Invalid(node, $"a Round needs places from 0 to {MaxPlaces}");
        }

        return node.Mode is { } mode && Array.IndexOf(ModeNames, mode) >= 0
            ? GenericResult.Success()
            : Invalid(node, $"a Round needs a mode, one of {string.Join(", ", ModeNames)}");
    }

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        if (emission.Node.Places is not { } places || emission.Node.Mode is not { } modeName || !Enum.TryParse<MidpointRounding>(modeName, false, out var mode))
        {
            throw new InvalidOperationException("A Round node without places or a known mode reached Emit; Check refuses it");
        }

        return Expression.Call(Method, emission.Operands[0], Expression.Constant(places), Expression.Constant(mode));
    }
}
