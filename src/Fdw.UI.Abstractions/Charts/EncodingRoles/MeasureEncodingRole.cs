using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Single numeric measure — the primary value field for KPI tiles and donut totals.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Measure")]
public sealed class MeasureEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeasureEncodingRole"/> class.
    /// </summary>
    public MeasureEncodingRole()
        : base(9, "Measure", "Measure", isSpatial: false)
    {
    }
}
