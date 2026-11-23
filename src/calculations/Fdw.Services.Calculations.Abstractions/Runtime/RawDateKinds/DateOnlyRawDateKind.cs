using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>A calendar date.</summary>
[TypeOption(typeof(RawDateKinds), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateOnlyRawDateKind : RawDateKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateOnlyRawDateKind"/> class.</summary>
    public DateOnlyRawDateKind()
        : base("Date")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object? raw) => raw is DateOnly;

    /// <inheritdoc />
    public override DateOnly Read(object? raw, string field, CalculationRunState state, int node)
    => (DateOnly)raw!;
}
