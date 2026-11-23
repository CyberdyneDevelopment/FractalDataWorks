using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>A date and time: a date only when it has no time of day, which is what the Date data type holds.</summary>
[TypeOption(typeof(RawDateKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeRawDateKind : RawDateKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeRawDateKind"/> class.</summary>
    public DateTimeRawDateKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object? raw) => raw is DateTime;

    /// <inheritdoc />
    public override DateOnly Read(object? raw, string field, CalculationRunState state, int node)
    {
        var moment = (DateTime)raw!;
        return moment.TimeOfDay == TimeSpan.Zero
            ? DateOnly.FromDateTime(moment)
            : state.FailDateWith(CalculationRuntime.FieldNotDateId, node, $"field '{field}' holds {moment:O}, which has a time of day; a date has none");
    }
}
