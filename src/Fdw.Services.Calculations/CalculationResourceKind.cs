using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Workspaces.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// A calculation attached to a workspace.
/// </summary>
/// <remarks>
/// Declared here rather than in the workspaces package because this package owns the calculation.
/// A host that has not referenced calculations cannot attach one to a workspace.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceResourceKinds), "Calculation")]
public sealed class CalculationResourceKind : WorkspaceResourceKindBase
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
