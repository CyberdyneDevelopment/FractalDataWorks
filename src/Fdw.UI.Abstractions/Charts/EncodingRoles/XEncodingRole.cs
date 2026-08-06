using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Horizontal axis encoding role.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "X")]
public sealed class XEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XEncodingRole"/> class.
    /// </summary>
    public XEncodingRole()
        : base(1, "X", "X Axis", isSpatial: true)
    {
    }
}
