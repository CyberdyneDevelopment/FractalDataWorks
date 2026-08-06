using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Vertical axis encoding role.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Y")]
public sealed class YEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YEncodingRole"/> class.
    /// </summary>
    public YEncodingRole()
        : base(2, "Y", "Y Axis", isSpatial: true)
    {
    }
}
