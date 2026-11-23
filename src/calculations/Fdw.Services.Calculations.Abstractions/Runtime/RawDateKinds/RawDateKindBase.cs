using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.Runtime;

/// <summary>The base of every raw date kind.</summary>
[ExcludeFromCodeCoverage]
public abstract class RawDateKindBase : TypeOptionBase<RawDateKindBase>, IRawDateKind
{
    /// <summary>Initializes a new instance of the <see cref="RawDateKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected RawDateKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object? raw);

    /// <inheritdoc />
    public abstract DateOnly Read(object? raw, string field, CalculationRunState state, int node);
}
