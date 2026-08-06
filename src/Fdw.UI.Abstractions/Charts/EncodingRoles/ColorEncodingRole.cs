using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Colour-coding channel — maps a field value to a colour scale or palette.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Color")]
public sealed class ColorEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColorEncodingRole"/> class.
    /// </summary>
    public ColorEncodingRole()
        : base(4, "Color", "Color", isSpatial: false)
    {
    }
}
