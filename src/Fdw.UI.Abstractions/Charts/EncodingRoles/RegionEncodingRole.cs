using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Geographic region field — binds to a named-region (country, state, postal code) for choropleth maps.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Region")]
public sealed class RegionEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegionEncodingRole"/> class.
    /// </summary>
    public RegionEncodingRole()
        : base(6, "Region", "Region", isSpatial: false)
    {
    }
}
