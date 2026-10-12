using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// True when both numbers have a value and the values differ; false when either is empty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "NotEqual", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NotEqualNode : ComparisonNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotEqualNode"/> class.
    /// </summary>
    public NotEqualNode()
        : base("NotEqual", "True when both numbers have a value and the values differ; false when either is empty.")
    {
    }
}
