using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A bool.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Bool")]
public sealed class BoolRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="BoolRecordValueShape"/> class.</summary>
    public BoolRecordValueShape()
        : base("Bool")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is bool;

    /// <inheritdoc />
    public override bool IsTrue(object value) => (bool)value;
}
