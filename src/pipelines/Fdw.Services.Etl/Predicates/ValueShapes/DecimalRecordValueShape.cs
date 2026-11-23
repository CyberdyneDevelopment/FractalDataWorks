using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A decimal.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Decimal")]
public sealed class DecimalRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalRecordValueShape"/> class.</summary>
    public DecimalRecordValueShape()
        : base("Decimal")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is decimal;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (decimal)value;
        return true;
    }
}
