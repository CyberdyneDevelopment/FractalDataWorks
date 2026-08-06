using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Tooltip channel — additional field surfaced in hover tooltips only, not mapped to a visible axis or glyph.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Tooltip")]
public sealed class TooltipEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipEncodingRole"/> class.
    /// </summary>
    public TooltipEncodingRole()
        : base(13, "Tooltip", "Tooltip", isSpatial: false)
    {
    }
}
