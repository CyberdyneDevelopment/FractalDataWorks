using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Input kind representing a Container as the source of calculation input.
/// </summary>
[TypeOption(typeof(CalculationInputKinds), "Container")]
[ExcludeFromCodeCoverage]
public sealed class ContainerCalculationInputKind : CalculationInputKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerCalculationInputKind"/> class.
    /// </summary>
    public ContainerCalculationInputKind()
        : base(id: 2, name: "Container")
    {
    }
}
