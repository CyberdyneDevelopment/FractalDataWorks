using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A text, as it is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ParameterTextShapes), "Text")]
public sealed class TextParameterTextShape : ParameterTextShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="TextParameterTextShape"/> class.</summary>
    public TextParameterTextShape()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is string;

    /// <inheritdoc />
    public override string Write(object value) => (string)value;
}
