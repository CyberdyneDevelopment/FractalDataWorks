using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>System.DateTime</c>: 8 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "DateTime")]
public sealed class DateTimeRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeRowValueShape"/> class.</summary>
    public DateTimeRowValueShape()
        : base("DateTime", typeof(System.DateTime), 8)
    {
    }
}
