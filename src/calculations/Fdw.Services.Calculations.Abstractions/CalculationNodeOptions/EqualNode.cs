using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when both numbers have a value and the values are equal; false when either is empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Equal", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class EqualNode : ComparisonNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EqualNode"/> class.
    /// </summary>
    public EqualNode()
        : base("Equal", "True when both numbers have a value and the values are equal; false when either is empty.")
    {
    }
}
