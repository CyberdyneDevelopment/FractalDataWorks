using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A short.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Short")]
public sealed class ShortRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="ShortRecordValueShape"/> class.</summary>
    public ShortRecordValueShape()
        : base("Short")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is short;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (short)value;
        return true;
    }
}
