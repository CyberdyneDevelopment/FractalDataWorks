using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when both numbers have a value and the first is less; false when either is empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Less", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LessNode : ComparisonNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessNode"/> class.
    /// </summary>
    public LessNode()
        : base("Less", "True when both numbers have a value and the first is less; false when either is empty.")
    {
    }
}
