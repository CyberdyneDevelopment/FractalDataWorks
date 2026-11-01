using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Lineage;

/// <summary>Toward what the focus feeds.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageDirections), "Down")]
public sealed class DownLineageDirection : LineageDirectionBase
{
    /// <summary>Initializes a new instance of the <see cref="DownLineageDirection"/> class.</summary>
    public DownLineageDirection()
        : base("Down", "down", false, true)
    {
    }
}
