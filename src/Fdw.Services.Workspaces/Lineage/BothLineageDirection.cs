using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Lineage;

/// <summary>Both ways from the focus.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageDirections), "Both")]
public sealed class BothLineageDirection : LineageDirectionBase
{
    /// <summary>Initializes a new instance of the <see cref="BothLineageDirection"/> class.</summary>
    public BothLineageDirection()
        : base("Both", "both", true, true)
    {
    }
}
