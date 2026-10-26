using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.RelativeTime;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// A date-time plus a signed whole amount of seconds, minutes, hours, days, weeks, months or years. The amount is an operand, so a
/// calculation can take it as an input.
/// </summary>
/// <remarks>
/// Seconds, minutes and hours are elapsed time. Days, weeks, months and years are calendar time in the stated time zone, so one
/// day before the start of today is the start of yesterday on the day the clocks change too.
/// </remarks>
[TypeOption(typeof(CalculationNodeTypes), "AddTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AddTimeNode : RelativeTimeNodeBase
{
    private static readonly string[] Names = { "time", "amount" };

    /// <summary>
    /// Initializes a new instance of the <see cref="AddTimeNode"/> class.
    /// </summary>
    public AddTimeNode()
        : base("AddTime", "A date-time plus a signed whole amount of seconds, minutes, hours, days, weeks, months or years.", 2)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> OperandNames => Names;

    /// <inheritdoc />
    protected override IReadOnlyList<string> ModeNames => TimeUnits.All().Select(unit => unit.Name).ToList();

    /// <inheritdoc />
    protected override string ModeWord => "a unit";

    /// <inheritdoc />
    public override Type? OperandType(int index) => index == 0 ? typeof(DateTimeOffset) : typeof(decimal);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        var unit = emission.Node.Mode is { } name
            ? TimeUnits.ByName(name)
            : throw new InvalidOperationException("An AddTime node without a unit reached Emit; Check refuses it");
        return NodeEmission.Call(
            nameof(CalculationRuntime.AddTime),
            Expression.Constant(unit, typeof(ITimeUnit)),
            emission.Operands[0],
            NodeEmission.Call(nameof(CalculationRuntime.WholeAmount), emission.Operands[1], emission.State, emission.Index),
            Expression.Constant(ZoneOf(emission.Node), typeof(TimeZoneInfo)),
            emission.State,
            emission.Index);
    }
}
