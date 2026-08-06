using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Flow magnitude field for Sankey links. Determines the width of each flow band.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Weight")]
public sealed class WeightEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeightEncodingRole"/> class.
    /// </summary>
    public WeightEncodingRole()
        : base(12, "Weight", "Weight", isSpatial: false)
    {
    }
}
