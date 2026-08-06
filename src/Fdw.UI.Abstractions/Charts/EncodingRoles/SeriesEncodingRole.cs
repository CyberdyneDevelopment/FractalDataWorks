using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Series grouping dimension — splits data into multiple series by the bound field value.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Series")]
public sealed class SeriesEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesEncodingRole"/> class.
    /// </summary>
    public SeriesEncodingRole()
        : base(3, "Series", "Series", isSpatial: false)
    {
    }
}
