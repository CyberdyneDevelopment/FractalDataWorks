using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Input kind representing a scalar (literal) value as the source of calculation input.
/// </summary>
[TypeOption(typeof(CalculationInputKinds), "Scalar")]
[ExcludeFromCodeCoverage]
public sealed class ScalarCalculationInputKind : CalculationInputKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScalarCalculationInputKind"/> class.
    /// </summary>
    public ScalarCalculationInputKind()
        : base(id: 3, name: "Scalar")
    {
    }
}
