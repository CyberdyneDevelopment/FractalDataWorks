using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Dataverses.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// A calculation attached to a dataverse.
/// </summary>
/// <remarks>
/// Declared here rather than in the dataverses package because this package owns the calculation.
/// A host that has not referenced calculations cannot attach one to a dataverse.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceKinds), "Calculation")]
public sealed class CalculationResourceKind : DataverseResourceKindBase
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
