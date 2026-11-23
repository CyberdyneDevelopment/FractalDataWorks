using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A long.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Long")]
public sealed class LongRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="LongRecordValueShape"/> class.</summary>
    public LongRecordValueShape()
        : base("Long")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is long;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (long)value;
        return true;
    }
}
