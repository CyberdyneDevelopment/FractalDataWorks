using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>Any value no other shape matches: it counts as true and reads as a number only through its text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Plain")]
public sealed class PlainRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="PlainRecordValueShape"/> class.</summary>
    public PlainRecordValueShape()
        : base("Plain")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => false;
}
