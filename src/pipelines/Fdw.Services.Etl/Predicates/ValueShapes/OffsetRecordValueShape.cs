using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A DateTimeOffset.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Offset")]
public sealed class OffsetRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="OffsetRecordValueShape"/> class.</summary>
    public OffsetRecordValueShape()
        : base("Offset")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override bool TryMoment(object value, out DateTimeOffset moment)
    {
        moment = (DateTimeOffset)value;
        return true;
    }
}
