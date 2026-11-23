using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Predicates;

/// <summary>A string: true when it reads as true.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RecordValueShapes), "Text")]
public sealed class TextRecordValueShape : RecordValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TextRecordValueShape"/> class.</summary>
    public TextRecordValueShape()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is string;

    /// <inheritdoc />
    public override bool IsTrue(object value) => bool.TryParse((string)value, out var parsed) && parsed;
}
