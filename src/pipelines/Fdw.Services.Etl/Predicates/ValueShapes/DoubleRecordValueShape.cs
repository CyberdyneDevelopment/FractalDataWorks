using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A double.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Double")]
public sealed class DoubleRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleRecordValueShape"/> class.</summary>
    public DoubleRecordValueShape()
        : base("Double")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is double;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (decimal)(double)value;
        return true;
    }
}
