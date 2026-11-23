using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>Nothing: no value, a database null, or text that is blank.</summary>
[TypeOption(typeof(RawDateKinds), "Empty")]
[ExcludeFromCodeCoverage]
public sealed class EmptyRawDateKind : RawDateKindBase
{
    /// <summary>Initializes a new instance of the <see cref="EmptyRawDateKind"/> class.</summary>
    public EmptyRawDateKind()
        : base("Empty")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object? raw) => raw is null || raw is DBNull || (raw is string text && string.IsNullOrWhiteSpace(text));

    /// <inheritdoc />
    public override DateOnly Read(object? raw, string field, CalculationRunState state, int node)
    => state.FailDateWith(CalculationRuntime.EmptyOperandId, node, $"date field '{field}' is empty");
}
