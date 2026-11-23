using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A byte array: its length.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Bytes")]
public sealed class BytesRowValueShape : RowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="BytesRowValueShape"/> class.</summary>
    public BytesRowValueShape()
        : base("Bytes", typeof(byte[]))
    {
    }

    /// <inheritdoc />
    public override long SizeOf(object value) => ((byte[])value).Length;
}
