using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>System.Guid</c>: 16 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Guid")]
public sealed class GuidRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="GuidRowValueShape"/> class.</summary>
    public GuidRowValueShape()
        : base("Guid", typeof(System.Guid), 16)
    {
    }
}
