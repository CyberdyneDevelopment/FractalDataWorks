using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A int.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Int")]
public sealed class IntRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="IntRecordValueShape"/> class.</summary>
    public IntRecordValueShape()
        : base("Int")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is int;

    /// <inheritdoc />
    public override bool TryDecimal(object value, out decimal number)
    {
        number = (int)value;
        return true;
    }
}
