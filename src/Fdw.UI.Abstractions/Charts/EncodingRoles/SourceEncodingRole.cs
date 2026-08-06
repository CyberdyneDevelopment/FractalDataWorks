using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Source node field for flow diagrams (Sankey). Identifies where a link originates.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Source")]
public sealed class SourceEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceEncodingRole"/> class.
    /// </summary>
    public SourceEncodingRole()
        : base(10, "Source", "Source", isSpatial: false)
    {
    }
}
