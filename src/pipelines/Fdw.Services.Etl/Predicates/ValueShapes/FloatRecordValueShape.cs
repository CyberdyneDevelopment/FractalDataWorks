using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A float.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Float")]
public sealed class FloatRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatRecordValueShape"/> class.</summary>
    public FloatRecordValueShape()
        : base("Float")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is float;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (decimal)(float)value;
        return true;
    }
}
