using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>A bool: true or false.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ParameterTextShapes), "Flag")]
public sealed class FlagParameterTextShape : ParameterTextShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlagParameterTextShape"/> class.</summary>
    public FlagParameterTextShape()
        : base("Flag")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is bool;

    /// <inheritdoc />
    public override string Write(object value) => (bool)value ? "true" : "false";
}
