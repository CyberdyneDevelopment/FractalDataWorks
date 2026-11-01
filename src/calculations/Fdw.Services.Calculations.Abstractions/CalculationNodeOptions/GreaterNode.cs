using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when both numbers have a value and the first is greater; false when either is empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Greater", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GreaterNode : ComparisonNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterNode"/> class.
    /// </summary>
    public GreaterNode()
        : base("Greater", "True when both numbers have a value and the first is greater; false when either is empty.")
    {
    }
}
