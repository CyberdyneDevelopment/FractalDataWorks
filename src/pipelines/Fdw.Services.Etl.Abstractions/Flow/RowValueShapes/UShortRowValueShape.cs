using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A value of type <c>ushort</c>: 2 bytes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "UShort")]
public sealed class UShortRowValueShape : FixedRowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="UShortRowValueShape"/> class.</summary>
    public UShortRowValueShape()
        : base("UShort", typeof(ushort), 2)
    {
    }
}
