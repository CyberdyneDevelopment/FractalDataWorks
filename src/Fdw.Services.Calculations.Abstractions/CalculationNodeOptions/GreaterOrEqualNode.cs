using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when both numbers have a value and the first is not less; false when either is empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "GreaterOrEqual", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GreaterOrEqualNode : ComparisonNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterOrEqualNode"/> class.
    /// </summary>
    public GreaterOrEqualNode()
        : base("GreaterOrEqual", "True when both numbers have a value and the first is not less; false when either is empty.")
    {
    }
}
