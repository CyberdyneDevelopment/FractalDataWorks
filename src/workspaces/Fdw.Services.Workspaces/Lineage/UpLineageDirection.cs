using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Lineage;

/// <summary>Toward what feeds the focus.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageDirections), "Up")]
public sealed class UpLineageDirection : LineageDirectionBase
{
    /// <summary>Initializes a new instance of the <see cref="UpLineageDirection"/> class.</summary>
    public UpLineageDirection()
        : base("Up", "up", true, false)
    {
    }
}
