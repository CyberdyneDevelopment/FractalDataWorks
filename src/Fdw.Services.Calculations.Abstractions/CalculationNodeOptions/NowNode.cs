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
/// The current date-time in a stated time zone, optionally cut back to the start of the day, week, month or year.
/// </summary>
/// <remarks>
/// With no <c>mode</c> the node yields the moment as it is. The time zone only matters once an anchor reads the calendar, but it is
/// required either way so a calculation states one zone throughout.
/// <para>
/// The moment comes from the clock the evaluation was given and is taken once per evaluation, so two Now nodes in one calculation
/// agree. An evaluation given no clock fails with ClockMissing.
/// </para>
/// </remarks>
[TypeOption(typeof(CalculationNodeTypes), "Now", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NowNode : RelativeTimeNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NowNode"/> class.
    /// </summary>
    public NowNode()
        : base("Now", "The current date-time in a stated time zone, optionally cut back to the start of the day, week, month or year.", 0)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<string> ModeNames => TimeAnchors.All().Select(anchor => anchor.Name).ToList();

    /// <inheritdoc />
    protected override string ModeWord => "an anchor";

    /// <inheritdoc />
    protected override bool ModeIsOptional => true;

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission)
    {
        var now = NodeEmission.Call(nameof(CalculationRuntime.Now), emission.State, emission.Index);
        return emission.Node.Mode is { } name
            ? NodeEmission.Call(
                nameof(CalculationRuntime.Anchor),
                Expression.Constant(TimeAnchors.ByName(name), typeof(ITimeAnchor)),
                now,
                Expression.Constant(ZoneOf(emission.Node), typeof(TimeZoneInfo)),
                emission.State,
                emission.Index)
            : now;
    }
}
