using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A byte.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Byte")]
public sealed class ByteRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="ByteRecordValueShape"/> class.</summary>
    public ByteRecordValueShape()
        : base("Byte")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is byte;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (byte)value;
        return true;
    }
}
