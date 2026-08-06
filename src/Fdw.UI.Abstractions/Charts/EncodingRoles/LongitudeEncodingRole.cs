using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Longitude coordinate field — used with <c>Latitude</c> for point-based geo charts.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Longitude")]
public sealed class LongitudeEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LongitudeEncodingRole"/> class.
    /// </summary>
    public LongitudeEncodingRole()
        : base(8, "Longitude", "Longitude", isSpatial: true)
    {
    }
}
