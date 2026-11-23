using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A text: two bytes a character.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RowValueShapes), "Text")]
public sealed class TextRowValueShape : RowValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TextRowValueShape"/> class.</summary>
    public TextRowValueShape()
        : base("Text", typeof(string))
    {
    }

    /// <inheritdoc />
    public override long SizeOf(object value) => 2L * ((string)value).Length;
}
