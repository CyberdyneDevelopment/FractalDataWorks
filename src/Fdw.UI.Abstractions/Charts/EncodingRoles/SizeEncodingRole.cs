using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Charts.EncodingRoles;

/// <summary>
/// Size channel — maps a numeric field to marker or bubble radius.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ChartEncodingRoles), "Size")]
public sealed class SizeEncodingRole : ChartEncodingRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SizeEncodingRole"/> class.
    /// </summary>
    public SizeEncodingRole()
        : base(5, "Size", "Size", isSpatial: false)
    {
    }
}
