using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Target node field for flow diagrams (Sankey). Identifies where a link terminates.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Target")]
public sealed class TargetEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetEncodingRole"/> class.
    /// </summary>
    public TargetEncodingRole()
        : base(11, "Target", "Target", isSpatial: false)
    {
    }
}
