using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Latitude coordinate field — used with <c>Longitude</c> for point-based geo charts.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Latitude")]
public sealed class LatitudeEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LatitudeEncodingRole"/> class.
    /// </summary>
    public LatitudeEncodingRole()
        : base(7, "Latitude", "Latitude", isSpatial: true)
    {
    }
}
