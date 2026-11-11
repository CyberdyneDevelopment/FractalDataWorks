using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.RelativeTime;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// A date plus a signed whole amount of days, weeks, months or years. A date goes in and a date comes out; there is no time of day,
/// so there is no time zone.
/// </summary>
/// <remarks>
/// Why a node of its own and not <c>AddTime</c>: a moment needs a time zone to count calendar units and can also move by elapsed
/// time; a date has neither a zone nor hours, so reading it as a moment would invent both.
/// </remarks>
[TypeOption(typeof(CalculationNodeTypes), "AddDate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AddDateNode : CalculationNodeBase
{
    private static readonly string[] Names = { "date", "amount" };
    private static readonly string[] MemberNames = { "mode" };

    /// <summary>
    /// Initializes a new instance of the <see cref="AddDateNode"/> class.
    /// </summary>
    public AddDateNode()
        : base("AddDate", "A date plus a signed whole amount of days, weeks, months or years.", 2, 2)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> OperandNames => Names;

    /// <inheritdoc />
    public override IReadOnlyList<string> Members => MemberNames;

    /// <inheritdoc />
    public override IReadOnlyList<string> Modes => TimeUnits.All().OfType<IDateUnit>().Select(unit => unit.Name).ToList();

    /// <inheritdoc />
    public override Type? OperandType(int index) => index == 0 ? typeof(DateOnly) : typeof(decimal);

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => typeof(DateOnly);

    /// <inheritdoc />
    public override IGenericResult Check(CalculationNode node, ICalculationScope scope)
    {
        if (node.Mode is not { } unit)
        {
            return Invalid(node, $"an AddDate needs a unit in 'mode', one of {string.Join(", ", Modes)}; none is assumed");
        }

        return Modes.Contains(unit, StringComparer.Ordinal)
            ? GenericResult.Success()
            : Invalid(node, $"'{unit}' is not a unit that moves a date; use one of {string.Join(", ", Modes)}");
    }

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        var unit = emission.Node.Mode is { } name && TimeUnits.ByName(name) is IDateUnit dateUnit
            ? dateUnit
            : throw new InvalidOperationException("An AddDate node without a date unit reached Emit; Check refuses it");
        return NodeEmission.Call(
            nameof(CalculationRuntime.AddDate),
            Expression.Constant(unit, typeof(IDateUnit)),
            emission.Operands[0],
            NodeEmission.Call(nameof(CalculationRuntime.WholeAmount), emission.Operands[1], emission.State, emission.Index),
            emission.State,
            emission.Index);
    }
}
