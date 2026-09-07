using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Universes.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// A calculation attached to a universe.
/// </summary>
/// <remarks>
/// Declared here rather than in the universes package because this package owns the calculation.
/// A host that has not referenced calculations cannot attach one to a universe.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseResourceKinds), "Calculation")]
public sealed class CalculationResourceKind : UniverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculationResourceKind"/> class.</summary>
    /// <remarks>
    /// Why <c>canBeOwned: true</c>: a calculation is an expression authored against the project's
    /// own data, not a shared facility other projects read through.
    /// </remarks>
    public CalculationResourceKind()
        : base("Calculation", canBeOwned: true)
    {
    }
}
