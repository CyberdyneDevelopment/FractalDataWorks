using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when both numbers have a value and the first is not greater; false when either is empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "LessOrEqual", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LessOrEqualNode : ComparisonNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessOrEqualNode"/> class.
    /// </summary>
    public LessOrEqualNode()
        : base("LessOrEqual", "True when both numbers have a value and the first is not greater; false when either is empty.")
    {
    }
}
